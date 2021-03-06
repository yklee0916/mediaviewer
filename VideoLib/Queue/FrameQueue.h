#pragma once

#ifdef __cplusplus
#define __STDC_CONSTANT_MACROS
#ifdef _STDINT_H
#undef _STDINT_H
#endif
# include "stdint.h"
#endif

#include "PacketQueue.h"
#include "..\Frame\VideoFrame.h"
#include "..\Frame\AudioFrame.h"
#include "..\Frame\Packet.h"
#include "..\Video\IVideoDecoder.h"

using namespace System::Collections::Generic;
using namespace System::Threading;
using namespace System::Diagnostics;

namespace VideoLib {


	public ref class FrameQueue
	{
	public:

		enum class FrameQueueState {
			PLAY,      
			PAUSE,		
			BLOCK,
			CLOSE				
		};
						
		event EventHandler ^IsBufferingChanged;
		event EventHandler ^Finished;	

		enum class QueueID {
			DEMUXER = 0,
			VIDEO,
			AUDIO,		
		};

	private:
		
		static int nrPackets = 500;	
		static int minNrBufferedPackets = 100;
		
		FrameQueueState state;

		Object ^lockObject;

		IVideoDecoder *videoDecoder;

		VideoFrame ^videoFrame, ^convertedVideoFrame;
		AudioFrame ^audioFrame, ^convertedAudioFrame;
		Packet ^audioPacket;
		
		PacketQueue ^videoPackets;
		PacketQueue ^audioPackets;
		PacketQueue ^freePackets;	
			
		array<Packet ^> ^packetData;

		double videoClock;
		double audioClock;
		
		bool isSingleFrame;
		bool isLastFrame;

		bool isBuffering;	
		int nrBufferedPackets;
		int nrLastPacketsRead;
		int nrLastPackets;

		int nrVideoPacketReadErrors;
		int nrAudioPacketReadErrors;

		void synchronizeVideo(VideoFrame ^frame) {
		
			if(frame->AVLibFrameData->pts != AV_NOPTS_VALUE) {

				VideoLib::Stream *videoStream = videoDecoder->getStream(videoDecoder->getVideoStreamIndex());
				
				// convert pts to seconds, and shift by start_time
				int64_t startTime = videoStream->getStartTime();
								
				frame->Pts = videoStream->getTimeSeconds(frame->AVLibFrameData->pts - startTime);
				frame->Dts = videoStream->getTimeSeconds(frame->AVLibFrameData->pkt_dts - startTime);
				// set clock to current pts;
				videoClock = frame->Pts;

			} else {

				// if we aren't given a pts, set it to the clock 
				frame->Pts = videoClock;
			}

			// update the video clock to the pts of the next frame
			double frameDelay = av_q2d(videoDecoder->getVideoStream()->time_base);
			// if we are repeating a frame, adjust clock accordingly 
			frameDelay += frame->AVLibFrameData->repeat_pict * (frameDelay * 0.5);
			videoClock += frameDelay;
			
		}

		void synchronizeAudio(AudioFrame ^frame) {
		
			if(frame->AVLibFrameData->pts != AV_NOPTS_VALUE) {

				VideoLib::Stream *audioStream = videoDecoder->getStream(videoDecoder->getAudioStreamIndex());

				// convert pts to seconds, and shift by start_time
				int64_t startTime = audioStream->getStartTime();	

				frame->Pts = audioStream->getTimeSeconds(frame->AVLibFrameData->pts - startTime);
				frame->Dts = audioStream->getTimeSeconds(frame->AVLibFrameData->pkt_dts - startTime);

				// set clock to current pts;
				audioClock = frame->Pts;

			} else {

				// if we aren't given a pts, set it to the clock 
				frame->Pts = audioClock;
				// calculate next pts in seconds
				audioClock += frame->Length / double(videoDecoder->getAudioBytesPerSecond());
			}
		
		}

		void setPacketQueueState(PacketQueue::PacketQueueState videoState, PacketQueue::PacketQueueState audioState, 
			PacketQueue::PacketQueueState freeState) 
		{					
			if(videoDecoder->hasVideo()) {

				videoPackets->State = videoState;
			} 
							
			if(videoDecoder->hasAudio()) {

				audioPackets->State = audioState;
			} 
			
			freePackets->State = freeState;										
		}

		bool isPacketQueueInState(PacketQueue::PacketQueueState videoState, PacketQueue::PacketQueueState audioState, 
			PacketQueue::PacketQueueState freeState) 
		{
			bool isState = true;

			if(videoPackets->State != PacketQueue::PacketQueueState::CLOSE_END) {

				isState = videoPackets->State == videoState && isState;
			} 
							
			if(audioPackets->State != PacketQueue::PacketQueueState::CLOSE_END) {

				isState = audioPackets->State == audioState && isState;
			} 
			
			isState = freePackets->State == freeState && isState;
			
			return(isState);
		}

		PacketQueue::PacketQueueState frameToPacketState(FrameQueueState state, bool isStart) {
		
			switch(state) 
			{
			case FrameQueueState::PLAY:
				{
					return(PacketQueue::PacketQueueState::OPEN);
				}
			case FrameQueueState::PAUSE:
				{
					if(isStart) return(PacketQueue::PacketQueueState::PAUSE_START);
					else return(PacketQueue::PacketQueueState::PAUSE_END);
				}
			case FrameQueueState::BLOCK:
				{
					if(isStart) return(PacketQueue::PacketQueueState::BLOCK_START);
					else return(PacketQueue::PacketQueueState::BLOCK_END);
				}
			case FrameQueueState::CLOSE:
				{
					if(isStart) return(PacketQueue::PacketQueueState::CLOSE_START);
					else return(PacketQueue::PacketQueueState::CLOSE_END);
				}
			default:
				{
					throw gcnew InvalidOperationException();
				}

			}
		}

		// always called while framequeue is locked
		void isPacketQueueFinished(Object ^sender, EventArgs ^e) 
		{						
			bool isVideoFinished = videoPackets->State == PacketQueue::PacketQueueState::CLOSE_END;
			bool isAudioFinished = audioPackets->State == PacketQueue::PacketQueueState::CLOSE_END;

			if(isVideoFinished && isAudioFinished)
			{
				// stop free packets queue 
				freePackets->State = PacketQueue::PacketQueueState::CLOSE_END;
									
				Finished(this, EventArgs::Empty);					
			}
						
		}	

		// always called while framequeue is locked
		void startPacketQueueBuffering(Object ^sender, EventArgs ^e)
		{			
			bool isVideoBuffering = videoPackets->NrPacketsInQueue == 0 && 
				!(videoPackets->State == PacketQueue::PacketQueueState::CLOSE_END);
			bool isAudioBuffering = audioPackets->NrPacketsInQueue == 0 && 
				!(audioPackets->State == PacketQueue::PacketQueueState::CLOSE_END);

			if(isVideoBuffering || isAudioBuffering) {

				IsBuffering = true;
		
				setPacketQueueState(
					PacketQueue::PacketQueueState::PAUSE_START, 
					PacketQueue::PacketQueueState::PAUSE_START,
					PacketQueue::PacketQueueState::OPEN);										
			}
		}

		// always called while framequeue is locked
		void packetIsAddedToPacketQueue(Object ^sender, PacketType e)
		{			
			if(IsBuffering) 
			{
				nrBufferedPackets++;

				bool isVideoReady = videoPackets->NrPacketsInQueue > 0 || 
					videoPackets->State == PacketQueue::PacketQueueState::CLOSE_END;
				bool isAudioReady = audioPackets->NrPacketsInQueue > 0 || 
					audioPackets->State == PacketQueue::PacketQueueState::CLOSE_END;

				if(e == PacketType::LAST_PACKET) {

					nrLastPacketsRead++;
				}

				// stop buffering when atleast minNrBufferedPackets are read or the lastpacket(s)
				if(isVideoReady && isAudioReady && nrBufferedPackets >= minNrBufferedPackets 
					|| nrLastPacketsRead == nrLastPackets || videoPackets->NrPacketsInQueue == videoPackets->MaxPackets ||
					audioPackets->NrPacketsInQueue == audioPackets->MaxPackets) 
				{

					nrBufferedPackets = 0;
					IsBuffering = false;
									
					setPacketQueueState(
						PacketQueue::PacketQueueState::OPEN, 
						PacketQueue::PacketQueueState::OPEN,
						PacketQueue::PacketQueueState::OPEN);	
				}
			}
		}

		void endSingleFrame() {

			Monitor::Enter(lockObject);
			try {

				isSingleFrame = false;
				videoPackets->State = PacketQueue::PacketQueueState::PAUSE_START;
				audioPackets->State = PacketQueue::PacketQueueState::PAUSE_START;
				freePackets->State = PacketQueue::PacketQueueState::OPEN;

			} finally {

				Monitor::Exit(lockObject);
			}
						
		}
						
	public:

		void setState(FrameQueueState video, FrameQueueState audio, FrameQueueState demuxer) 
		{

			Monitor::Enter(lockObject);
			try {
										
				setPacketQueueState(frameToPacketState(video, true), frameToPacketState(audio, true), frameToPacketState(demuxer, true));
			
				// wait until the packet queue is fully closed	
				while(!isPacketQueueInState(frameToPacketState(video, false), frameToPacketState(audio, false), frameToPacketState(demuxer, false))) 
				{					
					Monitor::Wait(lockObject);
				}				

			} finally {
				Monitor::Exit(lockObject);
			}
		}
								
		FrameQueue() {

			lockObject = gcnew Object();
			
			videoFrame = nullptr;
			audioFrame = nullptr;

			convertedVideoFrame = nullptr;
			convertedAudioFrame = nullptr;
									
			videoPackets = gcnew PacketQueue("videoPackets", nrPackets, lockObject);
			audioPackets = gcnew PacketQueue("audioPackets", nrPackets, lockObject);
			freePackets = gcnew PacketQueue("freePackets",videoPackets->MaxPackets + audioPackets->MaxPackets,lockObject);
					
			videoPackets->StartBuffering += gcnew EventHandler(this,&FrameQueue::startPacketQueueBuffering);
			audioPackets->StartBuffering += gcnew EventHandler(this,&FrameQueue::startPacketQueueBuffering);

			videoPackets->AddedPacket += gcnew EventHandler<PacketType>(this,&FrameQueue::packetIsAddedToPacketQueue);
			audioPackets->AddedPacket += gcnew EventHandler<PacketType>(this,&FrameQueue::packetIsAddedToPacketQueue);

			videoPackets->IsFinished += gcnew EventHandler(this,&FrameQueue::isPacketQueueFinished);
			audioPackets->IsFinished += gcnew EventHandler(this,&FrameQueue::isPacketQueueFinished);

			packetData = gcnew array<Packet ^>(freePackets->MaxPackets);

			for(int i = 0; i < packetData->Length; i++) {

				packetData[i] = gcnew Packet();
			}												
				
			audioPacket = nullptr;

			nrVideoPacketReadErrors = 0;
			nrAudioPacketReadErrors = 0;
					
			isSingleFrame = false;
			isBuffering = false;		
		}

		~FrameQueue() {

			release();

			for(int i = 0; i < packetData->Length; i++) {

				delete packetData[i];
			}
			
		}

		property bool IsBuffering
		{		
			bool get() {

				return(isBuffering);
			}	

		private:
			void set(bool value) 
			{
				if(isBuffering != value) {

					isBuffering = value;
					IsBufferingChanged(this,EventArgs::Empty);
				}
			}
		}

		property int NrVideoPacketReadErrors
		{
			int get() {

				return(nrVideoPacketReadErrors);
			}

			void set(int value)
			{
				nrVideoPacketReadErrors = value;
			}
		}

		property int NrAudioPacketReadErrors
		{
			int get() {

				return(nrAudioPacketReadErrors);
			}

			void set(int value)
			{
				nrAudioPacketReadErrors = value;
			}
		}


		property PacketQueue::PacketQueueState VideoPacketQueueState {

			PacketQueue::PacketQueueState get() {

				return(videoPackets->State);
			}
		}
		
		property PacketQueue::PacketQueueState FreePacketQueueState {

			PacketQueue::PacketQueueState get() {

				return(freePackets->State);
			}
		}

		property PacketQueue::PacketQueueState AudioPacketQueueState {

			PacketQueue::PacketQueueState get() {

				return(audioPackets->State);
			}
		}

		property int MaxFreePackets {

			int get() {

				return(freePackets->MaxPackets);
			}
		}

		property int MaxAudioPackets {

			int get() {

				return(audioPackets->MaxPackets);
			}
		}

		property int MaxVideoPackets {

			int get() {

				return(videoPackets->MaxPackets);
			}
		}
		
		property int NrFreePacketsInQueue {

			int get() {

				return(freePackets->NrPacketsInQueue);
			}
		}

		property int NrVideoPacketsInQueue {

			int get() {

				return(videoPackets->NrPacketsInQueue);
			}
		}

		property int NrAudioPacketsInQueue {

			int get() {

				return(audioPackets->NrPacketsInQueue);
			}
		}

		property int MinNrBufferedPackets {

			int get() {

				return(minNrBufferedPackets);
			}

			void set(int value) {

				if(value < 0) value = 0;
				if(value > MaxFreePackets) value = MaxFreePackets;
				
				minNrBufferedPackets = value;
			}
		}
		
		void initialize(IVideoDecoder *videoDecoder) {

			release();

			this->videoDecoder = videoDecoder;			

			videoClock = 0;
			audioClock = 0;

			nrLastPackets = 0;

			if(videoDecoder->hasVideo()) 
			{
				videoFrame = gcnew VideoFrame();

				convertedVideoFrame = gcnew VideoFrame(
					videoDecoder->getWidth(), 
					videoDecoder->getHeight(), 
					videoDecoder->getOutputPixelFormat());

				nrLastPackets++; 

				videoPackets->reset();

			} else {

				videoPackets->State = PacketQueue::PacketQueueState::CLOSE_END;
			}
			
			if(videoDecoder->hasAudio()) 
			{
				audioFrame = gcnew AudioFrame();

				convertedAudioFrame = gcnew AudioFrame(
					videoDecoder->getOutputSampleFormat(),
					videoDecoder->getOutputChannelLayout(),
					videoDecoder->getOutputSampleRate());

				nrLastPackets++; 

				audioPackets->reset();

			} else {

				audioPackets->State = PacketQueue::PacketQueueState::CLOSE_END;
			}

			for(int i = 0; i < packetData->Length; i++) {

				freePackets->addPacket(packetData[i]);
			}
			
			freePackets->reset();

			audioPacket = nullptr;

			nrVideoPacketReadErrors = 0;
			nrAudioPacketReadErrors = 0;

			IsBuffering = false;
			nrBufferedPackets = 0;
			nrLastPacketsRead = 0;
			
			isSingleFrame = false;
		}
		
		void flush() {		

			Monitor::Enter(lockObject);
			try {
				
				// clear queues
				freePackets->flush();
				audioPackets->flush();
				videoPackets->flush();

				// add all packets into freepackets queue
				for(int i = 0; i < packetData->Length; i++) {

					packetData[i]->free();
					freePackets->addPacket(packetData[i]);					
				}

				IsBuffering = false;
				nrBufferedPackets = 0;
				nrLastPacketsRead = 0;
				isSingleFrame = false;
				audioPacket = nullptr;

			} finally {

				Monitor::Exit(lockObject);
			}
		
		}
		
		void startSingleFrame() {

			Monitor::Enter(lockObject);
			try {
				
				isSingleFrame = true;
				videoPackets->State = PacketQueue::PacketQueueState::OPEN;
				audioPackets->State = PacketQueue::PacketQueueState::OPEN;
				freePackets->State = PacketQueue::PacketQueueState::OPEN;

			} finally {

				Monitor::Exit(lockObject);
			}
						
		}
					
		void release() {

			IsBuffering = false;
			nrBufferedPackets = 0;
			isSingleFrame = false;

			if(convertedVideoFrame != nullptr) {

				delete convertedVideoFrame;
				convertedVideoFrame = nullptr;
			}

			if(convertedAudioFrame != nullptr) {

				delete convertedAudioFrame;
				convertedAudioFrame = nullptr;
			}

			if(videoFrame != nullptr) {

				delete videoFrame;
				videoFrame = nullptr;
			}

			if(audioFrame != nullptr) {

				delete audioFrame;
				audioFrame = nullptr;
			}
			
			audioPackets->flush();
			videoPackets->flush();
			freePackets->flush();
			
			for(int i = 0; i < packetData->Length; i++) {

				if(packetData[i] != nullptr) {

					packetData[i]->free();
				}

			}

		}
		
		bool getFreePacket(Packet ^%packet) {

			PacketQueue::GetResult result = freePackets->getPacket(packet);

			return(result == PacketQueue::GetResult::SUCCESS);
		}

		void addFreePacket(Packet ^packet) {

			// free packet data before inserting it back into freepackets
			packet->free();
			freePackets->addPacket(packet);
		}

		void addVideoPacket(Packet ^packet) {

			videoPackets->addPacket(packet);
		}

		void addAudioPacket(Packet ^packet) {

			audioPackets->addPacket(packet);
		}

		VideoFrame ^getDecodedVideoFrame() {

			int frameFinished = 0;

			while(!frameFinished) {

				Packet ^videoPacket = nullptr;

				PacketQueue::GetResult result = videoPackets->getPacket(videoPacket);
				if(result != PacketQueue::GetResult::SUCCESS) 
				{																	
					return(nullptr);
				}
			
				videoFrame->setFrameDefaults();

				int ret = videoDecoder->decodeVideoFrame(videoFrame->AVLibFrameData, &frameFinished, 
					videoPacket->AVLibPacketData);
				if(ret < 0) {

					VideoInit::writeToLog(AV_LOG_WARNING, "could not decode video frame");
				}
			
				if(frameFinished)
				{					
					videoDecoder->convertVideoFrame(videoFrame->AVLibFrameData, convertedVideoFrame->AVLibFrameData);					

					synchronizeVideo(convertedVideoFrame);									
				}

				addFreePacket(videoPacket);
			}

			if(isSingleFrame) {

				endSingleFrame();
			}

			return(convertedVideoFrame);


		}


		AudioFrame ^getDecodedAudioFrame() {

			int frameFinished = 0;		
			
			// a single audio packet can contain multiple frames
			while(1) {

				if(audioPacket == nullptr 
					|| audioPacket->Type == VideoLib::PacketType::LAST_PACKET 
					|| audioPacket->AVLibPacketData->size == 0) 
				{

					if(audioPacket != nullptr) {

						if(audioPacket->Type == VideoLib::PacketType::LAST_PACKET) 
						{
							return(nullptr);
						}

						addFreePacket(audioPacket);					
					}

					PacketQueue::GetResult result = audioPackets->getPacket(audioPacket);
					if(result != PacketQueue::GetResult::SUCCESS) 
					{													
						return(nullptr);
					}
				
				}

				while(audioPacket->AVLibPacketData->size > 0) {

					audioFrame->setFrameDefaults();

					int bytesConsumed = videoDecoder->decodeAudioFrame(audioFrame->AVLibFrameData, 
						&frameFinished, audioPacket->AVLibPacketData);						
					if(bytesConsumed < 0) {
					
						// error decoding packet, skip and play silence
						frameFinished = true;
						audioPacket->AVLibPacketData->size = 0;

					} else {

						audioPacket->AVLibPacketData->size -= bytesConsumed;
						audioPacket->AVLibPacketData->data += bytesConsumed;
					}

					if(frameFinished)
					{									
						convertedAudioFrame->Length = videoDecoder->convertAudioFrame(audioFrame->AVLibFrameData, convertedAudioFrame->AVLibFrameData);											
					
						synchronizeAudio(convertedAudioFrame);					

						convertedAudioFrame->copyAudioDataToManagedMemory();

						// generate next pts for audio packets that contain multiple frames
						double audioDurationSec = (double)audioFrame->AVLibFrameData->nb_samples / videoDecoder->getAudioSamplesPerSecond();
							
						int64_t audioDuration = videoDecoder->getStream(audioPacket->AVLibPacketData->stream_index)->getTimeBaseUnits(audioDurationSec);

						audioPacket->AVLibPacketData->pts += audioDuration;	

						if(videoDecoder->getMediaType() == MediaType::AUDIO_MEDIA && isSingleFrame) {

							endSingleFrame();
						}

						return(convertedAudioFrame);
					}
				}
				
			}

		}


	};

}
