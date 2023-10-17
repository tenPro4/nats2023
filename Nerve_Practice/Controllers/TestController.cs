using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Nats_Server;
using Nerve_Practice.Models;
using System.Xml.Linq;

namespace Nerve_Practice.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController:ControllerBase
    {
        private readonly AppDbContext _ctx;
        internal readonly static MemoryCache NodeCache = new MemoryCache(new MemoryCacheOptions());
        internal readonly static MemoryCache NodeCache2 = new MemoryCache(new MemoryCacheOptions());
        private readonly static MemoryCacheEntryOptions memoryCacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(10))
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(60));

        public TestController(AppDbContext ctx)
        {
            _ctx = ctx;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var name = "test";
            var result = await NodeCache.GetOrCreateAsync<Node>((name), async entry =>
            {
                // Keep in cache for this time, reset time if accessed.
                entry.SlidingExpiration = TimeSpan.FromMinutes(30);

                var nodeModel = await _ctx.Node.Where(x => x.Name.ToLower() == name.ToLower()).FirstOrDefaultAsync();
                return nodeModel;
            }) ?? throw new Exception("fail to get node");

            Nats.PublishStreaming();

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get2([FromRoute]int id)
        {
            if (!NodeCache2.TryGetValue(id, out Node? nodeClass))
            {
                var nodeClassModel = await _ctx.Node.Where(x => x.Id == id).FirstOrDefaultAsync();
                if (nodeClassModel == null)
                    throw new Exception("Node class not found exception");
                nodeClass = nodeClassModel;
                AddToCache(nodeClass);
            }

            return Ok(nodeClass);
        }

        [HttpGet("clear")]
        public async Task<IActionResult> Clear()
        {
            NodeCache.Clear();
            NodeCache2.Clear();
            return Ok();
        }

        [HttpGet("remove/{id}")]
        public async Task<IActionResult> Remove([FromRoute] int id)
        {
            NodeCache2.Remove(id);
            return Ok();
        }

        internal void AddToCache(Node nodeClass)
        {
            NodeCache2.Set(nodeClass.Id, nodeClass, memoryCacheEntryOptions);
        }
    }
}
