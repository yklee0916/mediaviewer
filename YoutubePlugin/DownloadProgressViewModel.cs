﻿using MediaViewer.Infrastructure.Utils;
using MediaViewer.Infrastructure.Video.TranscodeOptions;
using MediaViewer.MediaDatabase;
using MediaViewer.Model.Media.Base;
using MediaViewer.Model.Mvvm;
using MediaViewer.Model.Utils;
using MediaViewer.Progress;
using Microsoft.Practices.Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YoutubePlugin.Item;

namespace YoutubePlugin
{
    class DownloadProgressViewModel : CancellableOperationProgressBase
    {
        VideoLib.VideoTranscoder videoTranscoder;

        public DownloadProgressViewModel()
        {
            WindowTitle = "Image Search Download";
            WindowIcon = "pack://application:,,,/ImageSearchPlugin;component/Resources/Icons/Search.ico";

            videoTranscoder = new VideoLib.VideoTranscoder();
            videoTranscoder.setLogCallback(muxingInfoCallback, true, VideoLib.VideoTranscoder.LogLevel.LOG_LEVEL_INFO);
        }

        public void startDownload(String outputPath, List<MediaItem> items)
        {
            TotalProgress = 0;
            TotalProgressMax = items.Count;
            
            foreach (YoutubeVideoItem item in items)
            {
                if (CancellationToken.IsCancellationRequested) return;

                YoutubeVideoStreamedItem videoStream, audioStream;
                item.getBestQualityStreams(out videoStream, out audioStream);

                if (videoStream == null)
                {
                    InfoMessages.Add("Skipping: " + item.Name + " no streams found");
                    continue;
                }

                YoutubeItemMetadata metadata = item.Metadata as YoutubeItemMetadata;

                String fullpath;
                String ext = "." + MediaFormatConvert.mimeTypeToExtension(metadata.MimeType);
                String filename = FileUtils.removeIllegalCharsFromFileName(item.Name, " ") + ext;

                try
                {
                    fullpath = FileUtils.getUniqueFileName(outputPath + "\\" + filename);
                }
                catch (Exception)
                {
                    fullpath = FileUtils.getUniqueFileName(outputPath + "\\" + "stream" + ext);
                }

                if (audioStream == null)
                {
                    singleStreamDownload(fullpath, videoStream);
                }
                else
                {
                    muxStreamsDownload(fullpath, videoStream, audioStream);
                }

                InfoMessages.Add("Finished: " + videoStream.Name + " -> " + fullpath);

                TotalProgress++;
            }

        }

        private void muxStreamsDownload(string fullpath, YoutubeVideoStreamedItem videoStream, 
            YoutubeVideoStreamedItem audioStream)
        {                    
            TotalProgress = 0;
            ItemProgressMax = 100;

            Dictionary<String, Object> options = new Dictionary<string, object>();
            options.Add("videoStreamMode", StreamOptions.Copy);
            options.Add("audioStreamMode", StreamOptions.Copy);
                           
            ItemProgress = 0;
            ItemInfo = "Downloading and muxing: " + videoStream.Name;

            try
            {
                videoTranscoder.transcode(videoStream.Location, fullpath, CancellationToken, options,
                    muxingProgressCallback, audioStream.Location);
            }
            catch (Exception e)
            {
                InfoMessages.Add("Error transcoding: " + e.Message);

                try
                {
                    File.Delete(fullpath);
                }
                catch (Exception ex)
                {
                    InfoMessages.Add("Error deleting: " + fullpath + " " + ex.Message);
                }
                    
                return;
            }
                    
            ItemProgress = 100;  
        }

        void singleStreamDownload(String fullpath, YoutubeVideoStreamedItem item)
        {
            
            FileStream outFile = null;

            try
            {
                outFile = new FileStream(fullpath, FileMode.Create);
                string mimeType;

                ItemProgressMax = 1;
                ItemProgress = 0;

                ItemInfo = "Downloading: " + fullpath;
                StreamUtils.readHttpRequest(new Uri(item.Location), outFile, out mimeType, CancellationToken, downloadProgressCallback);

                ItemProgressMax = 1;
                ItemProgress = 1;
             
                outFile.Close();
            }
            catch (Exception e)
            {
                InfoMessages.Add("Error downloading: " + fullpath + " " + e.Message);

                if (outFile != null)
                {
                    outFile.Close();
                    File.Delete(fullpath);
                }
                return;
            }

        }


        void downloadProgressCallback(long bytesDownloaded, long totalBytes)
        {
            ItemProgressMax = (int)totalBytes;
            ItemProgress = (int)bytesDownloaded;
        }

        void muxingProgressCallback(double progress)
        {
            ItemProgress = (int)(progress * 100);
        }

        void muxingInfoCallback(int logLevel, String message)
        {
            InfoMessages.Add(message);
        }

    }
}