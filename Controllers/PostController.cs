﻿using Azure.Core;
using Mercury_Backend.Contexts;
using Mercury_Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Mercury_Backend.Utils;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Mercury_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostController : ControllerBase
    {
        private readonly ModelContext context;
        public PostController(ModelContext modelContext)
        {
            context = modelContext;
        }
        // GET: api/<PostController>
        [HttpGet]
        public string Get([FromForm] string userId, [FromForm] int maxNumber=10, [FromForm] int pageNumber=1)
        {
            JObject msg = new JObject();
            try
            {
                List<NeedPost> postList = null;
                if(userId != null)
                {
                    postList = context.NeedPosts.Where(post => post.SenderId == userId)
                        .Include(post => post.Sender).ThenInclude(sender => sender.Avatar)
                        .OrderBy(post => post.Time).ToList();
                }
                else
                {
                    postList = context.NeedPosts.Include(post => post.Sender)
                        .ThenInclude(sender => sender.Avatar).OrderBy(post => post.Time).ToList();
                }
                List<SimplifiedPost> result = new List<SimplifiedPost>();
                for (int i = 0; i < maxNumber && i + (pageNumber - 1) * maxNumber < postList.Count(); ++i)
                {
                    result.Add(Simplify.SimplfyPost(postList[i + (pageNumber - 1) * maxNumber]));
                }

                msg["PostList"] = JToken.FromObject(result);
                msg["Code"] = "200";
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine(e.ToString());
                msg["Code"] = "500";
                msg["Description"] = "Internal exception happens";
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                msg["Code"] = "400";
                msg["Description"] = "Unknown exception happens";
            }
            return JsonConvert.SerializeObject(msg);
        }

        // GET api/<PostController>/5
        [HttpGet("{postId}")]
        public string Get(string postId)
        {
            JObject msg = new JObject();
            try
            {
                var post = context.NeedPosts.Where(p => p.Id == postId)
                    .Include(p => p.PostComments)
                    .ThenInclude(c => c.Sender)
                    .ThenInclude(s => s.Avatar)
                    .Include(p => p.PostImages)
                    .ThenInclude(pi => pi.Image)
                    .Include(p => p.Sender)
                    .ThenInclude(s => s.Avatar)
                    .Single();

                var imageList = new List<string>();
                foreach (var pi in post.PostImages)
                {
                    imageList.Add(pi.Image.Path);
                }

                var commmentList = new List<SimplifiedComment>();
                foreach (var c in post.PostComments)
                {
                    commmentList.Add(Simplify.SimplifyComment(c));
                }

                var postDetail = new PostDetail(post.Id, post.SenderId, post.Sender.Nickname, post.Title, post.Content, (DateTime)post.Time,
                    post.Sender.Avatar.Path, commmentList, imageList);
                msg["Post"] = JToken.FromObject(postDetail, new JsonSerializer()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
                msg["Code"] = "200";
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine(e.ToString());
                msg["Code"] = "500";
                msg["Description"] = "Internal exception happens";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                msg["Code"] = "400";
                msg["Description"] = "Unknown exception happens";
            }
            return JsonConvert.SerializeObject(msg);
        }

        // POST api/<PostController>
        [HttpPost]
        public string Post([FromForm] NeedPost post, [FromForm] List<IFormFile> photos)
        {
            JObject msg = new JObject();
            try
            {
                post.Id = Generator.GenerateId(12);
                if (photos != null)
                {
                    for (int i = 0; i < photos.Count(); ++i)
                    {
                        var imageStream = System.Drawing.Bitmap.FromStream(photos[i].OpenReadStream());
                        var image = new Medium();
                        image.Id = Generator.GenerateId(20);
                        image.Type = "IMAGE";
                        var path = "Media/Image/" + image.Id + ".png";
                        imageStream.Save(path);
                        image.Path = path;
                        context.Media.Add(image);

                        var postImage = new PostImage
                        {
                            ImageId = image.Id,
                            PostId = post.Id,
                            Position = "CENTER"
                        };
                        context.PostImages.Add(postImage);

                        post.PostImages.Add(postImage);
                    }
                }

                post.Time = DateTime.Now;
                context.NeedPosts.Add(post);
                context.SaveChanges();
                msg["Code"] = "201";
                msg["PostId"] = post.Id;
            }
            catch (ExternalException)
            {
                msg["Code"] = "500";
                msg["Description"] = "The image was saved with the wrong image format. Or the image was saved to the same file it was created from.";
            }
            catch (ArgumentException)
            {
                msg["Code"] = "415";
                msg["Description"] = "Unsupported media";
            }
            catch(Exception e)
            {
                msg["Code"] = "400";
                msg["Description"] = "Unknown exception happens";
                Console.WriteLine(e.ToString());
            }
            return JsonConvert.SerializeObject(msg);
        }

        // PUT api/<PostController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<PostController>/5
        [HttpDelete("{postId}")]
        public string Delete(string postId)
        {
            JObject msg = new JObject();
            try
            {
                var post = context.NeedPosts.Single(p => p.Id == postId);
                context.NeedPosts.Remove(post);
                context.SaveChanges();
                msg["Code"] = "200";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                msg["Code"] = "400";
                msg["Description"] = "Unknown exception happens";
            }
            return JsonConvert.SerializeObject(msg);
        }

        // GET api/post/<postId>/comment
        [HttpGet("{postId}/comment")]
        public string PostComment(string postId)
        {
            JObject msg = new JObject();
            try
            {
                var commentList = context.PostComments.Where(comment => comment.PostId == postId).ToList();
                msg["CommentList"] = JToken.FromObject(commentList);
                msg["Code"] = "200";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                msg["Code"] = "400";
                msg["Description"] = "Unknown exception happens";
            }
            return JsonConvert.SerializeObject(msg);
        }

        // POST api/post/<postId>/comment
        [HttpPost("{postId}/comment")]
        public string PostComment(string postId, [FromForm] PostComment comment)
        {
            JObject msg = new JObject();
            try
            {
                comment.Id = Generator.GenerateId(15);
                comment.Time = DateTime.Now;
                comment.PostId = postId;
                context.PostComments.Add(comment);
                context.SaveChanges();
                msg["Code"] = "201";
            }
            catch (DbUpdateException e)
            {
                Console.WriteLine(e.ToString());
                msg["Code"] = "403";
                msg["Description"] = "Cannot update database";
            }
            catch (DBConcurrencyException e)
            {
                Console.WriteLine(e.ToString());
                msg["Code"] = "500";
                msg["Description"] = "Fail to update database because of concurrent requests";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                msg["Code"] = "400";
                msg["Description"] = "Unknown exception happens";
            }
            return JsonConvert.SerializeObject(msg);
        }

        // DELETE api/post/<postId>/comment/<commentId>
        [HttpDelete("{postId}/comment/{commentId}")]
        public string DeleteComment(string postId, string commentId)
        {
            JObject msg = new JObject();
            try
            {
                var comment = context.PostComments.Where(c => c.Id == commentId).ToList();
                context.PostComments.Remove(comment[0]);
                context.SaveChanges();
                msg["Code"] = "200";
            }
            catch (DbUpdateException e)
            {
                Console.WriteLine(e.ToString());
                msg["Code"] = "403";
                msg["Description"] = "Cannot update database";
            }
            catch (DBConcurrencyException e)
            {
                Console.WriteLine(e.ToString());
                msg["Code"] = "500";
                msg["Description"] = "Fail to update database because of concurrent requests";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                msg["Code"] = "400";
                msg["Description"] = "Unknown exception happens";
            }
            return JsonConvert.SerializeObject(msg);
        }
        
        // GET api/post/postNumber
        [HttpGet("postNumber")]
        public string GetPostNumber([FromForm] string userId)
        {
            JObject msg = new JObject();
            try
            {
                int number;
                if (userId != null)
                {
                    number = context.NeedPosts.Count(p => p.SenderId == userId);
                }
                else
                {
                    number = context.NeedPosts.Count();
                }
                msg["Code"] = "200";
                msg["PostNumber"] = number;
            }
            catch (Exception e)
            {
                msg["Code"] = "400";
            }
            return JsonConvert.SerializeObject(msg);
        }
    }
}
