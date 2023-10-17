using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel.DataAnnotations;

namespace Nerve_Practice.Models
{
    public class Node
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FullPath { get; set; }
    }

}
