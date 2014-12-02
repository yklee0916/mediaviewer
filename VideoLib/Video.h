#pragma once
#pragma warning(disable : 4244)
// unsafe function warning disable
#pragma warning(disable : 4996)
#include <algorithm>
#include "stdafx.h"
#include "VideoInit.h"
#include <msclr\marshal.h>

extern "C" {

#ifdef __cplusplus
#define __STDC_CONSTANT_MACROS
#ifdef _STDINT_H
#undef _STDINT_H
#endif
# include "stdint.h"
#endif


#include "libavcodec/avcodec.h"

#include "libavformat/avformat.h"

#include "libavutil/avutil.h"
#include "libavutil/audioconvert.h"
#include "libavutil/mathematics.h"
#include "libavutil/pixdesc.h"

#include "libavutil/time.h"

#include "libswscale/swscale.h"
#include "libswresample/swresample.h"


#ifdef PixelFormat
#undef PixelFormat
#endif

}

typedef void (__stdcall *LOG_CALLBACK)(int level, const char *message);

class Video {

protected:

	AVFormatContext *formatContext;

	AVCodecContext *videoCodecContext;
	AVCodec *videoCodec;

	AVCodecContext *audioCodecContext;
	AVCodec *audioCodec;
	
	AVStream *videoStream;
	int videoStreamIndex;

	AVStream *audioStream;
	int audioStreamIndex;

	static LOG_CALLBACK logCallback;

	Video() {

		formatContext = NULL;

		videoCodecContext = NULL;
		videoCodec = NULL;

		audioCodecContext = NULL;
		audioCodec = NULL;

		videoStream = NULL;
		audioStream = NULL;

		videoStreamIndex = -1;
		audioStreamIndex = -1;

		VideoInit::initializeAVLib();
			
	}

	static void libAVLogCallback(void *ptr, int level, const char *fmt, va_list vargs)
    {
		char message[65536];   
		const char *module = NULL;

		// if no logging is done or level is above treshold return
		if(logCallback == NULL || level > av_log_get_level()) return;
	
		// Get module name
		if(ptr)
		{
			AVClass *avc = *(AVClass**) ptr;
			module = avc->item_name(ptr);
		}

		std::string fullMessage = "FFMPEG";

		if(module)
		{
			fullMessage.append(" (");
			fullMessage.append(module);
			fullMessage.append(")");
		}

		vsnprintf(message, sizeof(message), fmt, vargs);

		fullMessage.append(": ");
		fullMessage.append(message);

		// remove trailing newline
		fullMessage.erase(std::remove(fullMessage.begin(), fullMessage.end(), '\n'), fullMessage.end());

		logCallback(level, fullMessage.c_str());
	
    }
	
public:

	static System::String ^errorToString(int err)
	{
		char errbuf[128];
		const char *errbuf_ptr = errbuf;

		if (av_strerror(err, errbuf, sizeof(errbuf)) < 0)
			errbuf_ptr = strerror(AVUNERROR(err));
		
		return(msclr::interop::marshal_as<System::String^>(errbuf_ptr));
	}

	enum FrameType {
		VIDEO,
		AUDIO
	};

	virtual ~Video() {

		disableLibAVLogging();
		close();
	}

	virtual void close() {
		
		if(formatContext != NULL) {
					
			for (unsigned int i = 0; i < formatContext->nb_streams; i++) {

				avcodec_close(formatContext->streams[i]->codec);			
			}

			avformat_close_input(&formatContext);
		} 

		formatContext = NULL;
		videoCodecContext = NULL;
		audioCodecContext = NULL;

		videoStreamIndex = -1;
		audioStreamIndex = -1;

		videoStream = NULL;
		audioStream = NULL;

		videoCodec = NULL;
		audioCodec = NULL;

		videoCodecContext = NULL;
		audioCodecContext = NULL;
	
	}


	AVFormatContext *getFormatContext() const {

		return(formatContext);
	}

	AVCodecContext *getVideoCodecContext() const {

		return(videoCodecContext);
	}

	AVCodec *getVideoCodec() const {

		return(videoCodec);
	}

	AVCodecContext *getAudioCodecContext() const {

		return(audioCodecContext);
	}

	AVCodec *getAudioCodec() const {

		return(audioCodec);
	}
	
	AVStream *getVideoStream() const {

		return(videoStream);
	}

	int getVideoStreamIndex() const {

		return(videoStreamIndex);
	}

	AVStream *getAudioStream() const {

		return(audioStream);
	}

	int getAudioStreamIndex() const {

		return(audioStreamIndex);
	}

	void setFormatContext(AVFormatContext *formatContext) {

		this->formatContext = formatContext;
	}

	void setVideoCodecContext(AVCodecContext *videoCodecContext) {

		this->videoCodecContext = videoCodecContext;
	}

	void setVideoCodec(AVCodec *videoCodec) {

		this->videoCodec = videoCodec;
	}

	void setAudioCodecContext(AVCodecContext *audioCodecContext) {

		this->audioCodecContext = audioCodecContext;
	}

	void setAudioCodec(AVCodec *audioCodec) {

		this->audioCodec = audioCodec;
	}
	
	void setVideoStream(AVStream *videoStream) {

		this->videoStream = videoStream;
	}

	void setVideoStreamIndex(int videoStreamIndex) {

		this->videoStreamIndex = videoStreamIndex;
	}

	void setAudioStream(AVStream *audioStream) {

		this->audioStream = audioStream;
	}

	void setAudioStreamIndex(int audioStreamIndex) {

		this->audioStreamIndex = audioStreamIndex;
	}

	bool hasVideo() const {

		return(videoStreamIndex != -1);
	}

	bool hasAudio() const {

		return(audioStreamIndex != -1);
	}

	static void enableLibAVLogging(int logLevel = AV_LOG_ERROR) {
	
		av_log_set_level(logLevel); 
		av_log_set_callback(&Video::libAVLogCallback);			
			
	}

	static void disableLibAVLogging() {

		av_log_set_callback(NULL);	
	}

	static void setLogCallback(LOG_CALLBACK logCallback) 
	{
		Video::logCallback = logCallback;
	}

	static void writeToLog(int level, char *message) {

		if(logCallback != NULL) {

			logCallback(level, message);
		}
	}
};

LOG_CALLBACK Video::logCallback = NULL;

