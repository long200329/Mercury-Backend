using Mercury_Backend.Contexts;
using Mercury_Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Linq;

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Mercury_Backend.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos.Linq;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Mercury_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommodityController : ControllerBase
    {
        private ModelContext context;
        public CommodityController(ModelContext modelContext)
        {
            context = modelContext;
        }
        // GET: api/<CommodityController>
        // [HttpGet]
        // public string Get()
        // {
        //     JObject msg = new JObject();
        //     try
        //     {
        //         var commodityList = context.Commodities.OrderBy(b => b.Id).ToList<Commodity>();
        //         msg["commodityList"] = JToken.FromObject(commodityList);
        //         msg["status"] = "success";
        //     }
        //     catch(Exception e)
        //     {
        //         msg["status"] = "fail";
        //     }
        //     return JsonConvert.SerializeObject(msg);
        // }

        // GET api/<CommodityController>/5
        [HttpGet]
        public string Get()
        {
            var flag = 0;
            JObject msg = new JObject();
            var commodityList = new List<Commodity>();
            try
            {
                var judge = Request.Form["keyword"].ToString();
            }
            catch (Exception e)
            {
                msg["status"] = "fail";
                return JsonConvert.SerializeObject(msg);
            }

            
            if (Request.Form["keyword"].ToString() == "" != true)
            {
                var strKeyWord = Request.Form["keyword"].ToString();
                
                var tmpList = context.Commodities.Where(b => b.Name.Contains(strKeyWord)).ToList<Commodity>();
                // entering searching by keyword.
                var idList = tmpList.Select(s => new {s.Id});
                commodityList = tmpList;
                
                flag = 1;
                try
                {
                    msg["status"] = "success";
                }
                catch(Exception e)
                {
                    msg["status"] = "fail";
                }
                // 
                // todo: 用关键词搜索
            }
            
            
            else if (Request.Form["owner_name"].ToString() == "" != true)
            {
                var ownerName = Request.Form["owner_name"];
                var strOwnerName = ownerName.ToString();
                
                // msg["commodityList"] = JToken.FromObject(commodityList);
                
                var usrs = context.SchoolUsers.Where(b => b.Nickname.Contains(strOwnerName)).ToList();
                var idList = usrs.Select(s => new {s.SchoolId}).ToList();
                
                for (int i = 0; i < idList.Count; i++)
                {
                    var tmpList = context.Commodities.Where(b => b.OwnerId== idList[i].SchoolId).ToList<Commodity>();
                    commodityList = commodityList.Concat(tmpList).ToList<Commodity>();
                }
            }
            

            else if (Request.Form["tag"].ToString() == "" != true)
            {
                var strTagName = Request.Form["tag"].ToString();
                var tagList = context.CommodityTags.Where(b => b.Tag == strTagName);
                var idList = tagList.Select(s => new {s.CommodityId}).ToList();
                for (int i = 0; i < idList.Count; i++)
                {
                    var tmpList = context.Commodities.Where(b => idList[i].CommodityId == b.Id).ToList<Commodity>();
                    commodityList = commodityList.Concat(tmpList).ToList<Commodity>();
                }
            }

            else
            {
                msg["status"] = "fail";
                return JsonConvert.SerializeObject(msg);
            }
            try
            {
                msg["commodityList"] = JToken.FromObject(commodityList, new JsonSerializer()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore //忽略循环引用，默认是throw exception
                });
                var idList = commodityList.Select(s => s.Id).ToList();
                var tags = new List<CommodityTag>();
                for (int i = 0; i < idList.Count; i++)
                {
                    var tmpTag = context.CommodityTags.Where(tag => tag.CommodityId == idList[i])
                        .ToList();

                    tags = tags.Concat(tmpTag).ToList();
                    
                }

                var tagSet = tags.Select(s => s.Tag).ToList();
                tagSet = tagSet.Distinct().ToList();
                msg["tags"] = JToken.FromObject(tagSet);
                
                    
                msg["status"] = "success";
                msg["totalPage"] = commodityList.Count;
            }
            catch (Exception e)
            {
                msg["status"] = "fail";
            }
            return JsonConvert.SerializeObject(msg);
        }
        
        

        // POST api/<CommodityController>
        [HttpPost]
        public async Task<string> Post([FromForm]Commodity newCommodity, [FromForm] List<IFormFile> files)
        {
            JObject msg = new JObject();
            var id = Generator.GenerateId(12);
            newCommodity.Id = id;
            var pathList = new List<string>();
            try
            {
                Console.WriteLine(files.Count());
                if (files.Count() == 0)
                {
                    Console.WriteLine("No files uploaded.");
                }
                for (int i = 0; i < files.Count(); i++)
                {
                    var tmpVideoId = Generator.GenerateId(20);
                    var splitFileName = files[i].FileName.Split('.');
                    var len = splitFileName.Length;
                    var postFix = splitFileName[len - 1];
                    var path = "";
                    if (postFix == "jpg" || postFix == "jpeg" || postFix == "gif" || postFix == "png")
                    {
                        path = "Media" + "/Image/" + tmpVideoId + '.' + postFix;
                        if (Directory.Exists(path))
                        {
                            Console.WriteLine("This path exists.");
                        }
                        else
                        {
                            Directory.CreateDirectory("Media");
                            Directory.CreateDirectory("Media/Image");
                        }

                        var med = new Medium();
                        med.Id = tmpVideoId;
                        med.Type = "Image";
                        context.Media.Add(med);
                        var comImg = new CommodityImage
                        {
                            Commodity = newCommodity,
                            CommodityId = newCommodity.Id,
                            Image = med,
                            ImageId = med.Id
                        };
                        
                        using (var stream = new FileStream(path, FileMode.Create))
                        {
                            await files[i].CopyToAsync(stream);
                        }
                        // imageStream.Save(path);
                        newCommodity.CommodityImages.Add(comImg);
                    }
                    else if (postFix == "mov" || postFix == "mp4" || postFix == "wmv" || postFix == "rmvb" || postFix == "3gp")
                    {
                        path = "Media" + "/Video/" + tmpVideoId + '.' + postFix;
                        Console.WriteLine(path);
                        pathList.Add(path);
                        
                        if (Directory.Exists(path))
                        {
                            Console.WriteLine("This path exists.");
                        }
                        else
                        {
                            Directory.CreateDirectory("Media");
                            Directory.CreateDirectory("Media/Video");
                        }
                        using (var stream = new FileStream(path, FileMode.Create))
                        {
                            await files[i].CopyToAsync(stream);
                        }

                        var video = new Medium();
                        video.Id = tmpVideoId;
                        video.Type = "Video";
                        video.Path = path;
                        context.Media.Add(video);
                        newCommodity.VideoId = tmpVideoId;
                    }
                    else
                    {
                        Console.WriteLine("Not a media file.");
                    }
                }
                context.Commodities.Add(newCommodity);
                // Console.WriteLine("haha");
                context.SaveChanges();
                msg["status"] = "success";
            }
            catch (Exception e)
            {
                msg["status"] = "fail";
                Console.WriteLine(e.ToString());
            }
            return JsonConvert.SerializeObject(msg);
        }

        // PUT api/<CommodityController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<CommodityController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
