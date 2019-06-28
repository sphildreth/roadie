using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Enums;
using Roadie.Library.Identity;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using models = Roadie.Library.Models;

namespace Roadie.Api.Controllers
{
    [Produces("application/json")]
    [Route("comments")]
    [ApiController]
    [Authorize]
    public class CommentController : EntityControllerBase
    {
        private ICommentService CommentService { get; }

        public CommentController(ILoggerFactory logger, ICacheManager cacheManager, UserManager<ApplicationUser> userManager,
                                 IRoadieSettings roadieSettings, ICommentService commentService)
            : base(cacheManager, roadieSettings, userManager)
        {
            this.Logger = logger.CreateLogger("RoadieApi.Controllers.CommentController");
            this.CommentService = commentService;
        }

        [HttpPost("add/artist/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddNewArtistComment(Guid id, Comment model)
        {
            if (string.IsNullOrEmpty(model.Cmt))
            {
                return StatusCode((int)HttpStatusCode.BadRequest, "Invalid Comment");
            }
            var result = await this.CommentService.AddNewArtistComment(await this.CurrentUserModel(), id, model.Cmt);
            if (result == null || result.IsNotFoundResult)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("add/release/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddNewReleaseComment(Guid id, Comment model)
        {
            if (string.IsNullOrEmpty(model.Cmt))
            {
                return StatusCode((int)HttpStatusCode.BadRequest, "Invalid Comment");
            }
            var result = await this.CommentService.AddNewReleaseComment(await this.CurrentUserModel(), id, model.Cmt);
            if (result == null || result.IsNotFoundResult)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("add/track/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddNewTrackComment(Guid id, Comment model)
        {
            if (string.IsNullOrEmpty(model.Cmt))
            {
                return StatusCode((int)HttpStatusCode.BadRequest, "Invalid Comment");
            }
            var result = await this.CommentService.AddNewTrackComment(await this.CurrentUserModel(), id, model.Cmt);
            if (result == null || result.IsNotFoundResult)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("add/playlist/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddNewPlaylistComment(Guid id, Comment model)
        {
            if (string.IsNullOrEmpty(model.Cmt))
            {
                return StatusCode((int)HttpStatusCode.BadRequest, "Invalid Comment");
            }
            var result = await this.CommentService.AddNewPlaylistComment(await this.CurrentUserModel(), id, model.Cmt);
            if (result == null || result.IsNotFoundResult)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("add/label/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddNewLabelComment(Guid id, Comment model)
        {
            if (string.IsNullOrEmpty(model.Cmt))
            {
                return StatusCode((int)HttpStatusCode.BadRequest, "Invalid Comment");
            }
            var result = await this.CommentService.AddNewLabelComment(await this.CurrentUserModel(), id, model.Cmt);
            if (result == null || result.IsNotFoundResult)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("add/genre/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddNewGenreComment(Guid id, Comment model)
        {
            if (string.IsNullOrEmpty(model.Cmt))
            {
                return StatusCode((int)HttpStatusCode.BadRequest, "Invalid Comment");
            }
            var result = await this.CommentService.AddNewGenreComment(await this.CurrentUserModel(), id, model.Cmt);
            if (result == null || result.IsNotFoundResult)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("add/collection/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddNewCollectionComment(Guid id, Comment model)
        {
            if (string.IsNullOrEmpty(model.Cmt))
            {
                return StatusCode((int)HttpStatusCode.BadRequest, "Invalid Comment");
            }
            var result = await this.CommentService.AddNewCollectionComment(await this.CurrentUserModel(), id, model.Cmt);
            if (result == null || result.IsNotFoundResult)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("delete/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteComment(Guid id)
        {
            var result = await this.CommentService.DeleteComment(await this.CurrentUserModel(), id);
            if (result == null || result.IsNotFoundResult)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("setreaction/{id}/{reaction}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> SetCommentReaction(Guid id, CommentReaction reaction)
        {
            var result = await this.CommentService.SetCommentReaction(await this.CurrentUserModel(), id, reaction);
            if (result == null || result.IsNotFoundResult)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

    }
  
}