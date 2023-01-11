using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using studious_enigma.Data;
using studious_enigma.Models;

namespace studious_enigma.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ArticlesController : ControllerBase
    {
        private MemoryCryptContext _context;
        private readonly ILogger<ArticlesController> _logger;

        public ArticlesController(ILogger<ArticlesController> logger, MemoryCryptContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpPost]
        public ActionResult<Article> InsertArticle(Article article)
        {
            article.Id = Guid.NewGuid();
            _context.Articles.Add(article);
            _context.SaveChanges();
            return CreatedAtAction(nameof(GetArticles), new { id = article.Id }, article);
        }

        [HttpGet]
        public ActionResult<IEnumerable<Article>> GetArticles()
        {
            return Ok(_context.Articles.ToList());
        }

        [HttpGet("{id}")]
        public ActionResult<Article> GetArticles(string id)
        {
            var article = _context.Articles.FirstOrDefault(a => a.Id.Equals(id));
            if (article == null)
            {
                return NotFound();
            }


            return Ok(article);
        }

        [HttpPut("{id}")]
        public ActionResult<Article> UpdateArticle(string id, Article article)
        {
            if (id != article.Id.ToString())
            {
                return BadRequest();
            }

            var articleToUpdate = _context.Articles.FirstOrDefault(a => a.Id.Equals(id));

            if (articleToUpdate == null)
            {
                return NotFound();
            }

            articleToUpdate.Author = article.Author;
            articleToUpdate.Content = article.Content;
            articleToUpdate.Title = article.Title;
            articleToUpdate.UpVotes = article.UpVotes;
            articleToUpdate.Views = article.Views;

            return NoContent();
        }

        [HttpDelete("{id}")]
        public ActionResult DeleteArticle(string id)
        {
            var articleToDelete = _context.Articles.FirstOrDefault(a => a.Id.Equals(id));

            if (articleToDelete == null)
            {
                return NotFound();
            }

            _context.Articles.Remove(articleToDelete);

            return NoContent();
        }
    }
}