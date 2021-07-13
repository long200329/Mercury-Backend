﻿using Mercury_Backend.Contexts;
using Mercury_Backend.Models;
using Mercury_Backend.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Mercury_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly ModelContext context;
        public OrderController(ModelContext modelContext)
        {
            context = modelContext;
        }
        // GET: api/<OrderController>
        [HttpGet]
        public string Get([FromForm] string userId, [FromForm] int maxNumber = 10, [FromForm] int pageNumber = 1)
        {
            JObject msg = new JObject();
            try
            {
                List<Order> orderList = new List<Order>();
                if (userId != null)
                {
                    orderList = context.Orders.Where(order => order.BuyerId == userId)
                        .Include(order => order.Commodity)
                        .ThenInclude(commodity => commodity.CommodityImages)
                        .ThenInclude(commodityImages => commodityImages.Image)
                        .OrderByDescending(order => order.Time).ToList();
                }
                else
                {
                    orderList = context.Orders.Include(order => order.Commodity)
                        .ThenInclude(commodity => commodity.CommodityImages)
                        .ThenInclude(commodityImages => commodityImages.Image)
                        .OrderByDescending(order => order.Time).ToList();
                }

                var simplifiedOrderList = new List<SimplifiedOrder>();
                for (int i = 0; i + (pageNumber - 1) * maxNumber < orderList.Count() && i < maxNumber; ++i)
                {
                    simplifiedOrderList.Add(Simplify.SimplifyOrder(orderList[i + (pageNumber - 1) * maxNumber]));
                }

                msg["OrderList"] = JToken.FromObject(simplifiedOrderList);
                msg["Status"] = "200";
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine(e.ToString());
                msg["Status"] = "500";
                msg["Description"] = "Internal exception happens";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                msg["Status"] = "400";
                msg["Description"] = "Unknown exception";
            }
            return JsonConvert.SerializeObject(msg);
        }

        // GET api/<OrderController>/5
        [HttpGet("{id}")]
        public string Get(string id)
        {
            JObject msg = new JObject();
            try
            {
                var orderList = context.Orders.Where(order => order.Id == id).ToList<Order>();
                msg["order"] = JToken.FromObject(orderList);
                msg["Status"] = "200";
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine(e.ToString());
                msg["Status"] = "500";
                msg["Description"] = "Internal exception happens";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                msg["Status"] = "400";
                msg["Description"] = "Unknown exception";
            }
            return JsonConvert.SerializeObject(msg);
        }

        // POST api/<OrderController>
        [HttpPost]
        public string Post([FromForm] Order order)
        {
            JObject msg = new JObject();
            try
            {
                order.Id = Generator.GenerateId(20);
                order.Time = DateTime.Now;
                order.ReturnTime = Convert.ToDateTime(order.ReturnTime);
                order.Status = "UNPAID";
                context.Orders.Add(order);
                context.SaveChanges();
                msg["Status"] = "201";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                msg["Status"] = "400";
            }
            return JsonConvert.SerializeObject(msg);
        }

        // PUT api/<OrderController>/5
        [HttpPut("{id}")]
        public string Put(string id, [FromForm]string newStatus)
        {
            JObject msg = new JObject();
            if(newStatus != "PAID" && newStatus != "CANCELLED")
            {
                msg["Status"] = "403";
                msg["Description"] = "Cannot change status to unpaid";
                return JsonConvert.SerializeObject(msg);
            }
            try
            {
                var order = context.Orders.Single(o => o.Id == id);
                if(order.Status != "UNPAID")
                {
                    msg["Status"] = "403";
                    msg["Description"] = "Cannot update a paid or cancelled order";
                    return JsonConvert.SerializeObject(msg);
                }
                order.Status = newStatus;
                context.SaveChanges();
                msg["Status"] = "200";
            }
            catch (DbUpdateException e)
            {
                Console.WriteLine(e.ToString());
                msg["Status"] = "403";
                msg["Description"] = "Cannot update database";
            }
            catch (DBConcurrencyException e)
            {
                Console.WriteLine(e.ToString());
                msg["Status"] = "403";
                msg["Description"] = "Fail to update database because of concurrent requests";
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                msg["Status"] = "400";
                msg["Description"] = "Unknown exception";
            }
            return JsonConvert.SerializeObject(msg);
        }

        // DELETE api/<OrderController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        //GET api/order/<OrderId>/rating
        [HttpGet("{orderId}/rating")]
        public string GetRating(string orderId)
        {
            JObject msg = new JObject();
            try
            {
                var ratingList = context.Ratings.Where(rating => rating.OrderId == orderId).ToList<Rating>();
                msg["RatingList"] = JToken.FromObject(ratingList);
                msg["Status"] = "200";
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine(e.ToString());
                msg["Status"] = "500";
                msg["Description"] = "Internal exception happens";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                msg["Status"] = "400";
            }
            return JsonConvert.SerializeObject(msg);
        }

        //POST api/order/<OrderId>/rating
        [HttpPost("{id}/rating")]
        public string PostRating(string id, [FromForm]Rating rating)
        {
            JObject msg = new JObject();
            try
            {
                rating.RatingId = Generator.GenerateId(20);
                rating.OrderId = id;
                rating.Time = DateTime.Now;
                context.Ratings.Add(rating);
                context.SaveChanges();
                msg["Status"] = "201";
            }
            catch (DbUpdateException e)
            {
                Console.WriteLine(e.ToString());
                msg["Status"] = "403";
                msg["Description"] = "Cannot update database";
            }
            catch (DBConcurrencyException e)
            {
                Console.WriteLine(e.ToString());
                msg["Status"] = "403";
                msg["Description"] = "Fail to update database because of concurrent requests";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                msg["Status"] = "400";
            }
            return JsonConvert.SerializeObject(msg);
        }

        //DELETE api/order/<OrderId>/rating/<RatingId>
        [HttpDelete("{orderId}/rating/{ratingId}")]
        public string DeleteRating(string id, string ratingId)
        {
            JObject msg = new JObject();
            try
            {
                var rating = context.Ratings.Where(rating => rating.RatingId == ratingId).ToList<Rating>();
                context.Ratings.Remove(rating[0]);
                context.SaveChanges();
                msg["Status"] = "200";
            }
            catch (DbUpdateException e)
            {
                Console.WriteLine(e.ToString());
                msg["Status"] = "403";
                msg["Description"] = "Cannot update database";
            }
            catch (DBConcurrencyException e)
            {
                Console.WriteLine(e.ToString());
                msg["Status"] = "403";
                msg["Description"] = "Fail to update database because of concurrent requests";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                msg["Status"] = "400";
            }
            return JsonConvert.SerializeObject(msg);
        }
    }
}
