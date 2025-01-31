using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Mercury_Backend.Contexts;
using Mercury_Backend.Models;
using Mercury_Backend.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;

namespace Mercury_Backend.Controllers
{
   
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly ModelContext context;
        private readonly IConfiguration config;
        public ChatController(ModelContext modelContext, IConfiguration configuration)
        {
            context = modelContext;
            config = configuration;
        }
        // GET: api/<ChatController>/1/2
        [HttpGet("{userId}/{targetId}")]
        public String Get(string userId, string targetId)
        {
            JObject msg = new JObject();
            var list = context.ChatRecords
                .Where(record => record.SenderId == userId && record.ReceiverId == targetId)
                .OrderBy(record => record.Time);
            msg["Code"] = 200;
            msg["ChatRecord"] = JToken.FromObject(list);
            return JsonConvert.SerializeObject(msg);
        }

        // POST api/<ChatController>
        [HttpPost]
        public String Post([FromForm] string senderId, [FromForm] string receiverId, [FromForm] string content)
        {
            var list = context.ChatRecords
                .Where(record => record.SenderId == senderId && record.ReceiverId == receiverId);
            JObject msg = new JObject();
            try
            {
                var chatRecord = new ChatRecord()
                {
                    Content = content,
                    ReceiverId = receiverId,
                    SenderId = senderId,
                    Time = DateTime.Now,
                    Index = list.Count() + 1,
                    Status = "N"
                };
                context.ChatRecords.Add(chatRecord);
                context.SaveChanges();
                msg["Code"] = "200";
                msg["Description"] = "Message sent successfully.";
            }
            catch (DbUpdateException e)
            {
                Console.WriteLine(e.ToString());
                msg["Code"] = "403";
                msg["Description"] = "Cannot update database.";
            }
            catch (DBConcurrencyException e)
            {
                Console.WriteLine(e.ToString());
                msg["Code"] = "403";
                msg["Description"] = "Failed to update database because of concurrent requests.";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                msg["Code"] = "400";
            }
            return JsonConvert.SerializeObject(msg);
        }
    }
}