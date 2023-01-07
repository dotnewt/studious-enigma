using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using studious_enigma.Models;

namespace studious_enigma.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArticlesController : ControllerBase
    {
        private static List<Article> articles = new();
        private readonly ILogger<ArticlesController> _logger;

        public ArticlesController(ILogger<ArticlesController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public ActionResult<Article> InsertArticle(Article article)
        {
            article.Id = Guid.NewGuid().ToString();
            articles.Add(article);
            return CreatedAtAction(nameof(GetArticles), new { id = article.Id }, article);
        }

        [HttpGet]
        public ActionResult<IEnumerable<Article>> GetArticles()
        {
            return Ok(articles);
        }

        [HttpGet("{id}")]
        public ActionResult<Article> GetArticles(string id)
        {
            var article = articles.FirstOrDefault(a => a.Id.Equals(id));
            if (article == null)
            {
                return NotFound();
            }

            return Ok(article);
        }

        [HttpPut("{id}")]
        public ActionResult<Article> UpdateArticle(string id, Article article)
        {
            if (id != article.Id)
            {
                return BadRequest();
            }

            var articleToUpdate = articles.FirstOrDefault(a => a.Id.Equals(id));

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
            var articleToDelete = articles.FirstOrDefault(a => a.Id.Equals(id));

            if (articleToDelete == null)
            {
                return NotFound();
            }

            articles.Remove(articleToDelete);

            return NoContent();
        }
    }
}