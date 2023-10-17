using Microsoft.EntityFrameworkCore;
using Nats_Server;
using Nerve_Practice.Models;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;

services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("TestDB"));
services.AddControllers();

using (var scope = builder.Services.BuildServiceProvider().CreateScope())
{
    var _ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var check = _ctx.Node.Any();
    if (!check)
    {
        _ctx.Node.Add(new Node
        {
            Name = "test",
            FullPath = "fp"
        });

        _ctx.SaveChanges();
    }
}

var app = builder.Build();
app.UseRouting();
app.MapDefaultControllerRoute();
Task.Factory.StartNew(() => Nats.SubscribeStreaming("c2"), TaskCreationOptions.LongRunning);
app.Run();
