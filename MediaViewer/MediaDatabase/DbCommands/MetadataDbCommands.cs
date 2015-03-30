﻿using MediaViewer.Infrastructure.Logging;
using MediaViewer.Model.Media.File.Watcher;
using MediaViewer.Search;
using MediaViewer.UserControls.Relation;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Validation;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaViewer.MediaDatabase.DbCommands
{
    class MetadataDbCommands : DbCommands<BaseMetadata>
    {
        

        public MetadataDbCommands(MediaDatabaseContext existingContext = null) :
            base(existingContext)
        {
            
        }

        public List<BaseMetadata> getAllMetadata()
        {
            return(Db.BaseMetadataSet.ToList());
        }

        public int getNrMetadata()
        {
            int result = Db.BaseMetadataSet.Count();

            return (result);
        }

        public int getNrImageMetadata()
        {
            int result = Db.BaseMetadataSet.OfType<ImageMetadata>().Count();

            return (result);
        }

        public int getNrVideoMetadata()
        {
            int result = Db.BaseMetadataSet.OfType<VideoMetadata>().Count();

            return (result);
        }

        public int getNrMetadataInLocation(String location)
        {
            int result = Db.BaseMetadataSet.Where(m => m.Location.StartsWith(location)).Count();

            return (result);
        }

        public List<BaseMetadata> getMetadataInLocation(String location)
        {
            List<BaseMetadata> result = Db.BaseMetadataSet.Where(m => m.Location.StartsWith(location)).ToList();

            return (result);
        }

        public BaseMetadata findMetadataByLocation(String location)
        {            
            BaseMetadata result = Db.BaseMetadataSet.Include("Tags").FirstOrDefault(m => m.Location.Equals(location));
           
            return (result);
        }

        public List<BaseMetadata> findMetadataByQuery(SearchQuery query)
        {
            IQueryable<BaseMetadata> result = textQuery(query);

            result = tagQuery(result, query);

            if (result == null)
            {
                return (new List<BaseMetadata>());
            }

            // creation

            if (query.CreationStart != null && query.CreationEnd == null)
            {
                result = result.Where(m => m.CreationDate >= query.CreationStart.Value);
            }
            else if (query.CreationStart == null && query.CreationEnd!= null)
            {
                result = result.Where(m => m.CreationDate <= query.CreationEnd.Value);
            }
            else if (query.CreationStart != null && query.CreationEnd != null)
            {
                result = result.Where(m => (m.CreationDate >= query.CreationStart.Value) && (m.CreationDate <= query.CreationEnd.Value));
            }

            // rating

            if (query.RatingStart != null && query.RatingEnd == null)
            {
                result = result.Where(m => m.Rating >= query.RatingStart.Value * 5);
            }
            else if (query.RatingStart == null && query.RatingEnd != null)
            {
                result = result.Where(m => m.Rating <= query.RatingEnd.Value * 5);
            }
            else if (query.RatingStart != null && query.RatingEnd != null)
            {
                result = result.Where(m => (m.Rating >= query.RatingStart.Value * 5) && (m.Rating <= query.RatingEnd.Value * 5));
            }

            if (query.SearchType == MediaType.Video)
            {               
                result = videoQueryFilter(result, query);
            }
            else if (query.SearchType == MediaType.Images)
            {                
                result = imageQueryFilter(result, query);
            }
           
            return(result.ToList());
        }

        IQueryable<BaseMetadata> textQuery(SearchQuery query)
        {
            IQueryable<BaseMetadata> result = null;

            if (String.IsNullOrEmpty(query.Text) || String.IsNullOrWhiteSpace(query.Text))
            {
                return (result);
            }
           
            if (query.SearchType == MediaType.All)
            {

                result = Db.BaseMetadataSet.Include("Tags").Where(m =>
                       (m.Location.Contains(query.Text)) ||
                       (!String.IsNullOrEmpty(m.Title) && m.Title.Contains(query.Text)) ||
                       (!String.IsNullOrEmpty(m.Description) && m.Description.Contains(query.Text)) ||
                       (!String.IsNullOrEmpty(m.Author) && m.Author.Contains(query.Text)) ||
                       (!String.IsNullOrEmpty(m.Copyright) && m.Copyright.Contains(query.Text)) ||
                       (!String.IsNullOrEmpty(m.Software) && m.Software.Contains(query.Text))
                       );
            }
            else if (query.SearchType == MediaType.Images)
            {
                result = Db.BaseMetadataSet.Include("Tags").OfType<ImageMetadata>().Where(m =>
                       (m.Location.Contains(query.Text)) ||
                       (!String.IsNullOrEmpty(m.Title) && m.Title.Contains(query.Text)) ||
                       (!String.IsNullOrEmpty(m.Description) && m.Description.Contains(query.Text)) ||
                       (!String.IsNullOrEmpty(m.Author) && m.Author.Contains(query.Text)) ||
                       (!String.IsNullOrEmpty(m.Copyright) && m.Copyright.Contains(query.Text)) ||
                       (!String.IsNullOrEmpty(m.Software) && m.Software.Contains(query.Text)) ||
                       (!String.IsNullOrEmpty(m.CameraMake) && m.CameraMake.Contains(query.Text)) ||
                       (!String.IsNullOrEmpty(m.CameraModel) && m.CameraModel.Contains(query.Text)) ||
                       (!String.IsNullOrEmpty(m.Lens) && m.Lens.Contains(query.Text))
                       );
            }
            else if (query.SearchType == MediaType.Video)
            {              
                result = Db.BaseMetadataSet.Include("Tags").OfType<VideoMetadata>().Where(m =>
                        (m.Location.Contains(query.Text)) ||
                        (!String.IsNullOrEmpty(m.Title) && m.Title.Contains(query.Text)) ||
                        (!String.IsNullOrEmpty(m.Description) && m.Description.Contains(query.Text)) ||
                        (!String.IsNullOrEmpty(m.Author) && m.Author.Contains(query.Text)) ||
                        (!String.IsNullOrEmpty(m.Copyright) && m.Copyright.Contains(query.Text)) ||
                        (!String.IsNullOrEmpty(m.Software) && m.Software.Contains(query.Text)) ||
                        (!String.IsNullOrEmpty(m.AudioCodec) && m.AudioCodec.Contains(query.Text)) ||
                        (!String.IsNullOrEmpty(m.VideoCodec) && m.VideoCodec.Contains(query.Text)) ||
                        (!String.IsNullOrEmpty(m.VideoContainer) && m.VideoContainer.Contains(query.Text))
                        );
               
            }

            return (result);
        }

        IQueryable<BaseMetadata> tagQuery(IQueryable<BaseMetadata> result, SearchQuery query)
        {
            if (query.Tags.Count == 0)
            {
                return(result);
            }
            List<int> tagIds = new List<int>();
            foreach (Tag tag in query.Tags)
            {
                tagIds.Add(tag.Id);
            }

            if (result == null)
            {
                if (query.SearchType == MediaType.All)
                {
                    result = Db.BaseMetadataSet.Include("Tags").Where(m => m.Tags.Select(t => t.Id).Intersect(tagIds).Count() == tagIds.Count);
                }
                else if (query.SearchType == MediaType.Video)
                {
                    result = Db.BaseMetadataSet.Include("Tags").OfType<VideoMetadata>().Where(m => m.Tags.Select(t => t.Id).Intersect(tagIds).Count() == tagIds.Count);
                }
                else if (query.SearchType == MediaType.Images)
                {
                    result = Db.BaseMetadataSet.Include("Tags").OfType<ImageMetadata>().Where(m => m.Tags.Select(t => t.Id).Intersect(tagIds).Count() == tagIds.Count);
                }
            }
            else
            {
                result = result.Where(m => m.Tags.Select(t => t.Id).Intersect(tagIds).Count() == tagIds.Count);
            }
                          
            return (result);
        }


        IQueryable<BaseMetadata> imageQueryFilter(IQueryable<BaseMetadata> result, SearchQuery query)
        {
            // width

            if (query.ImageWidthStart != null && query.ImageWidthEnd == null)
            {
                result = result.OfType<ImageMetadata>().Where(m => m.Width >= query.ImageWidthStart.Value);
            }
            else if (query.ImageWidthStart == null && query.ImageWidthEnd != null)
            {
                result = result.OfType<ImageMetadata>().Where(m => m.Width <= query.ImageWidthEnd.Value);
            }
            else if (query.ImageWidthStart != null && query.ImageWidthEnd != null)
            {
                result = result.OfType<ImageMetadata>().Where(m => (m.Width >= query.ImageWidthStart.Value) && (m.Width <= query.ImageWidthEnd.Value));
            }

            // height

            if (query.ImageHeightStart != null && query.ImageHeightEnd == null)
            {
                result = result.OfType<ImageMetadata>().Where(m => m.Height >= query.ImageHeightStart.Value);
            }
            else if (query.ImageHeightStart == null && query.ImageHeightEnd != null)
            {
                result = result.OfType<ImageMetadata>().Where(m => m.Height <= query.ImageHeightEnd.Value);
            }
            else if (query.ImageHeightStart != null && query.ImageHeightEnd != null)
            {
                result = result.OfType<ImageMetadata>().Where(m => (m.Height >= query.ImageHeightStart.Value) && (m.Height <= query.ImageHeightEnd.Value));
            }           

            return (result);
        }

        IQueryable<BaseMetadata> videoQueryFilter(IQueryable<BaseMetadata> result, SearchQuery query)
        {
            // duration

            if (query.DurationSecondsStart != null && query.DurationSecondsEnd == null)
            {              
                result = result.OfType<VideoMetadata>().Where(m => m.DurationSeconds >= query.DurationSecondsStart.Value);
            }
            else if (query.DurationSecondsStart == null && query.DurationSecondsEnd != null)
            {               
                result = result.OfType<VideoMetadata>().Where(m => m.DurationSeconds <= query.DurationSecondsEnd.Value);
            }
            else if (query.DurationSecondsStart != null && query.DurationSecondsEnd != null)
            {
                result = result.OfType<VideoMetadata>().Where(m => (m.DurationSeconds >= query.DurationSecondsStart.Value) && (m.DurationSeconds <= query.DurationSecondsEnd.Value));
            }

            // width

            if (query.VideoWidthStart != null && query.VideoWidthEnd == null)
            {
                result = result.OfType<VideoMetadata>().Where(m => m.Width >= query.VideoWidthStart.Value);
            }
            else if (query.VideoWidthStart == null && query.VideoWidthEnd != null)
            {
                result = result.OfType<VideoMetadata>().Where(m => m.Width <= query.VideoWidthEnd.Value);
            }
            else if (query.VideoWidthStart != null && query.VideoWidthEnd != null)
            {
                result = result.OfType<VideoMetadata>().Where(m => (m.Width >= query.VideoWidthStart.Value) && (m.Width <= query.VideoWidthEnd.Value));
            }

            // height

            if (query.VideoHeightStart != null && query.VideoHeightEnd == null)
            {
                result = result.OfType<VideoMetadata>().Where(m => m.Height >= query.VideoHeightStart.Value);
            }
            else if (query.VideoHeightStart == null && query.VideoHeightEnd != null)
            {
                result = result.OfType<VideoMetadata>().Where(m => m.Height <= query.VideoHeightEnd.Value);
            }
            else if (query.VideoHeightStart != null && query.VideoHeightEnd != null)
            {
                result = result.OfType<VideoMetadata>().Where(m => (m.Height >= query.VideoHeightStart.Value) && (m.Height <= query.VideoHeightEnd.Value));
            }

            // frames per second

            if (query.FramesPerSecondStart != null && query.FramesPerSecondEnd == null)
            {
                result = result.OfType<VideoMetadata>().Where(m => m.FramesPerSecond >= query.FramesPerSecondStart.Value);
            }
            else if (query.FramesPerSecondStart == null && query.FramesPerSecondEnd != null)
            {
                result = result.OfType<VideoMetadata>().Where(m => m.FramesPerSecond <= query.FramesPerSecondEnd.Value);
            }
            else if (query.FramesPerSecondStart != null && query.FramesPerSecondEnd != null)
            {
                result = result.OfType<VideoMetadata>().Where(m => (m.FramesPerSecond >= query.FramesPerSecondStart.Value) && (m.FramesPerSecond <= query.FramesPerSecondEnd.Value));
            }

            // nr channels
            if (query.NrChannelsStart != null && query.NrChannelsEnd == null)
            {
                result = result.OfType<VideoMetadata>().Where(m => m.NrChannels >= query.NrChannelsStart.Value);
            }
            else if (query.NrChannelsStart == null && query.NrChannelsEnd != null)
            {
                result = result.OfType<VideoMetadata>().Where(m => m.NrChannels <= query.NrChannelsEnd.Value);
            }
            else if (query.NrChannelsStart != null && query.NrChannelsEnd != null)
            {
                result = result.OfType<VideoMetadata>().Where(m => (m.NrChannels >= query.NrChannelsStart.Value) && (m.NrChannels <= query.NrChannelsEnd.Value));
            }

            return result;
        }

    
        protected override BaseMetadata createFunc(BaseMetadata metadata)
        {
            if (String.IsNullOrEmpty(metadata.Location) || String.IsNullOrWhiteSpace(metadata.Location))
            {
                throw new DbEntityValidationException("Error creating metadata, location cannot be null, empty or whitespace");
            }

            if (Db.BaseMetadataSet.Any(t => t.Location == metadata.Location))
            {
                throw new DbEntityValidationException("Cannot create metadata with duplicate location: " + metadata.Location);
            }
           

            BaseMetadata newMedia = null;
            
            if (metadata is VideoMetadata)
            {
                VideoMetadata video = new VideoMetadata(metadata.Location, null);
                Db.BaseMetadataSet.Add(video);

                Db.Entry<VideoMetadata>(video).CurrentValues.SetValues(metadata);
                newMedia = video;
            }
            else
            {
                ImageMetadata image = new ImageMetadata(metadata.Location, null);
                Db.BaseMetadataSet.Add(image);

                Db.Entry<ImageMetadata>(image).CurrentValues.SetValues(metadata);
                newMedia = image;
            }
          
            FileInfo info = new FileInfo(metadata.Location);
            info.Refresh();

            if (info.LastWriteTime < (DateTime)SqlDateTime.MinValue)
            {

                Logger.Log.Warn("LastWriteTime for " + metadata.Location + " smaller as SqlDateTime.MinValue");
                newMedia.LastModifiedDate = (DateTime)SqlDateTime.MinValue;

            } else {

                newMedia.LastModifiedDate =  info.LastWriteTime;
            }
            
           
            newMedia.Id = 0;
    
            if (metadata.Thumbnail != null && metadata.Thumbnail.Id != 0)
            {
                //thumbnail already exists
                newMedia.Thumbnail = Db.ThumbnailSet.FirstOrDefault(t => t.Id == metadata.Thumbnail.Id);
                newMedia.Thumbnail.decodeImage();
            }
            else
            {
                if (metadata.Thumbnail != null)
                {
                    Db.ThumbnailSet.Add(metadata.Thumbnail);
                }
                newMedia.Thumbnail = metadata.Thumbnail;               
            }          

            TagDbCommands tagCommands = new TagDbCommands(Db);

            foreach (Tag tag in metadata.Tags)
            {               
                Tag result = tagCommands.getTagByName(tag.Name);

                if (result == null)
                {
                    Tag newTag = tagCommands.create(tag);
                    newTag.Used = 1;
                    newMedia.Tags.Add(newTag);
                }
                else
                {
                    result.Used += 1;                   
                    newMedia.Tags.Add(result);
                   
                }
               
            }

            Db.SaveChanges();

            newMedia.IsImported = true;

            return (newMedia);
        }

        protected override BaseMetadata updateFunc(BaseMetadata metadata)
        {
            if (String.IsNullOrEmpty(metadata.Location) || String.IsNullOrWhiteSpace(metadata.Location))
            {
                throw new DbEntityValidationException("Error updating metadata, location cannot be null, empty or whitespace");
            }
            
            BaseMetadata updateMedia = Db.BaseMetadataSet.FirstOrDefault(t => t.Id == metadata.Id);
            if (updateMedia == null)
            {
                throw new DbEntityValidationException("Cannot update non existing metadata: " + metadata.Id.ToString());
            }

            if (metadata is VideoMetadata)
            {
                Db.Entry<VideoMetadata>(updateMedia as VideoMetadata).CurrentValues.SetValues(metadata);                
            }
            else
            {
                Db.Entry<ImageMetadata>(updateMedia as ImageMetadata).CurrentValues.SetValues(metadata);      
            }

            FileInfo info = new FileInfo(metadata.Location);
            info.Refresh();
            updateMedia.LastModifiedDate = info.LastWriteTime;

            if (metadata.Thumbnail != null && metadata.Thumbnail.Id != 0)
            {
                //thumbnail already exists
                updateMedia.Thumbnail = Db.ThumbnailSet.FirstOrDefault(t => t.Id == metadata.Thumbnail.Id);
                updateMedia.Thumbnail.decodeImage();
            }
            else
            {
                if (updateMedia.Thumbnail != null)
                {
                    // remove old thumbnail
                    Db.ThumbnailSet.Remove(updateMedia.Thumbnail);
                }

                if (metadata.Thumbnail != null)
                {
                    Db.ThumbnailSet.Add(metadata.Thumbnail);
                }

                updateMedia.Thumbnail = metadata.Thumbnail;
            }

            TagDbCommands tagCommands = new TagDbCommands(Db);

            // remove tags
            for(int i = updateMedia.Tags.Count - 1; i >= 0; i--)
            {
                Tag tag = updateMedia.Tags.ElementAt(i);

                if (!metadata.Tags.Contains(tag, EqualityComparer<Tag>.Default))
                {                    
                    updateMedia.Tags.Remove(tag);
                    tag.Used -= 1;
                }
            }
            
            // add tags
            foreach (Tag tag in metadata.Tags)
            {
                Tag result = tagCommands.getTagByName(tag.Name);

                if (result == null)
                {
                    result = tagCommands.create(tag);                  
                }

                if (!updateMedia.Tags.Contains(result, EqualityComparer<Tag>.Default))
                {
                    updateMedia.Tags.Add(result);
                    result.Used += 1;
                }
            }
                       
            Db.SaveChanges();
                      
            updateMedia.IsImported = true;

            return (updateMedia);
        }

        protected override void deleteFunc(BaseMetadata metadata)
        {

            if (String.IsNullOrEmpty(metadata.Location) || String.IsNullOrWhiteSpace(metadata.Location))
            {
                throw new DbEntityValidationException("Error deleting metadata, location cannot be null, empty or whitespace");
            }

            BaseMetadata deleteMedia = findMetadataByLocation(metadata.Location);
            if (deleteMedia == null)
            {
                throw new DbEntityValidationException("Cannot delete non existing metadata: " + metadata.Location);
            }
        
            foreach (Tag tag in deleteMedia.Tags)
            {
                tag.Used -= 1;
            }

            if(deleteMedia.Thumbnail != null) {
                Db.ThumbnailSet.Remove(deleteMedia.Thumbnail);
                deleteMedia.Thumbnail = null;
            }

            Db.BaseMetadataSet.Remove(deleteMedia);
            Db.SaveChanges();

            metadata.Id = 0;
            if (metadata.Thumbnail != null)
            {
                metadata.Thumbnail.Id = 0;
                // make sure there is no lingering connection to the removed metadata entity
                // otherwise when we attach this thumbnail to a new entiy
                // the framework will bring in the (cached?) dead entity and mess things up
                metadata.Thumbnail.BaseMetadata = null;
            }
          

            metadata.IsImported = false;
        }

        public override void clearAll()
        {
            throw new NotImplementedException();

            /*String[] tableNames = new String[] {"ThumbnailSet", "MediaTag", "MediaSet_ImageMetadata",
                "MediaSet_VideoMetadata","MediaSet_UnknownMetadata","BaseMetadataSet"};

            for (int i = 0; i < tableNames.Count(); i++)
            {
                Db.Database.ExecuteSqlCommand("TRUNCATE TABLE [" + tableNames[i] + "]");
            }*/
           
        }
    }
}