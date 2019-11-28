using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Enums;
using Roadie.Library.Identity;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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

        public CommentController(ILogger<CommentController> logger, ICacheManager cacheManager,
                    UserManager<User> userManager,
            IRoadieSettings roadieSettings, ICommentService commentService)
            : base(cacheManager, roadieSettings, userManager)
        {
            Logger = logger;
            CommentService = commentService;
        }

        [HttpPost("add/artist/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddNewArtistComment(Guid id, models.Comment model)
        {
            if (string.IsNullOrEmpty(model.Cmt)) return StatusCode((int)HttpStatusCode.BadRequest, "Invalid Comment");
            var result = await CommentService.AddNewArtistComment(await CurrentUserModel(), id, model.Cmt);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess)
            {
                if (result.IsAccessDeniedResult)
                {
                    return StatusCode((int)HttpStatusCode.Forbidden);
                }
                if (result.Messages?.Any() ?? false)
                {
                    return StatusCode((int)HttpStatusCode.BadRequest, result.Messages);
                }
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("add/collection/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddNewCollectionComment(Guid id, models.Comment model)
        {
            if (string.IsNullOrEmpty(model.Cmt)) return StatusCode((int)HttpStatusCode.BadRequest, "Invalid Comment");
            var result = await CommentService.AddNewCollectionComment(await CurrentUserModel(), id, model.Cmt);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess)
            {
                if (result.IsAccessDeniedResult)
                {
                    return StatusCode((int)HttpStatusCode.Forbidden);
                }
                if (result.Messages?.Any() ?? false)
                {
                    return StatusCode((int)HttpStatusCode.BadRequest, result.Messages);
                }
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("add/genre/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddNewGenreComment(Guid id, models.Comment model)
        {
            if (string.IsNullOrEmpty(model.Cmt)) return StatusCode((int)HttpStatusCode.BadRequest, "Invalid Comment");
            var result = await CommentService.AddNewGenreComment(await CurrentUserModel(), id, model.Cmt);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess)
            {
                if (result.IsAccessDeniedResult)
                {
                    return StatusCode((int)HttpStatusCode.Forbidden);
                }
                if (result.Messages?.Any() ?? false)
                {
                    return StatusCode((int)HttpStatusCode.BadRequest, result.Messages);
                }
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("add/label/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddNewLabelComment(Guid id, models.Comment model)
        {
            if (string.IsNullOrEmpty(model.Cmt)) return StatusCode((int)HttpStatusCode.BadRequest, "Invalid Comment");
            var result = await CommentService.AddNewLabelComment(await CurrentUserModel(), id, model.Cmt);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess)
            {
                if (result.IsAccessDeniedResult)
                {
                    return StatusCode((int)HttpStatusCode.Forbidden);
                }
                if (result.Messages?.Any() ?? false)
                {
                    return StatusCode((int)HttpStatusCode.BadRequest, result.Messages);
                }
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("add/playlist/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddNewPlaylistComment(Guid id, models.Comment model)
        {
            if (string.IsNullOrEmpty(model.Cmt)) return StatusCode((int)HttpStatusCode.BadRequest, "Invalid Comment");
            var result = await CommentService.AddNewPlaylistComment(await CurrentUserModel(), id, model.Cmt);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess)
            {
                if (result.IsAccessDeniedResult)
                {
                    return StatusCode((int)HttpStatusCode.Forbidden);
                }
                if (result.Messages?.Any() ?? false)
                {
                    return StatusCode((int)HttpStatusCode.BadRequest, result.Messages);
                }
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("add/release/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddNewReleaseComment(Guid id, models.Comment model)
        {
            if (string.IsNullOrEmpty(model.Cmt)) return StatusCode((int)HttpStatusCode.BadRequest, "Invalid Comment");
            var result = await CommentService.AddNewReleaseComment(await CurrentUserModel(), id, model.Cmt);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess)
            {
                if (result.IsAccessDeniedResult)
                {
                    return StatusCode((int)HttpStatusCode.Forbidden);
                }
                if (result.Messages?.Any() ?? false)
                {
                    return StatusCode((int)HttpStatusCode.BadRequest, result.Messages);
                }
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("add/track/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddNewTrackComment(Guid id, models.Comment model)
        {
            if (string.IsNullOrEmpty(model.Cmt)) return StatusCode((int)HttpStatusCode.BadRequest, "Invalid Comment");
            var result = await CommentService.AddNewTrackComment(await CurrentUserModel(), id, model.Cmt);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess)
            {
                if (result.IsAccessDeniedResult)
                {
                    return StatusCode((int)HttpStatusCode.Forbidden);
                }
                if (result.Messages?.Any() ?? false)
                {
                    return StatusCode((int)HttpStatusCode.BadRequest, result.Messages);
                }
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("delete/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteComment(Guid id)
        {
            var result = await CommentService.DeleteComment(await CurrentUserModel(), id);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess)
            {
                if (result.IsAccessDeniedResult)
                {
                    return StatusCode((int)HttpStatusCode.Forbidden);
                }
                if (result.Messages?.Any() ?? false)
                {
                    return StatusCode((int)HttpStatusCode.BadRequest, result.Messages);
                }
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("setreaction/{id}/{reaction}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> SetCommentReaction(Guid id, CommentReaction reaction)
        {
            var result = await CommentService.SetCommentReaction(await CurrentUserModel(), id, reaction);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess)
            {
                if (result.IsAccessDeniedResult)
                {
                    return StatusCode((int)HttpStatusCode.Forbidden);
                }
                if (result.Messages?.Any() ?? false)
                {
                    return StatusCode((int)HttpStatusCode.BadRequest, result.Messages);
                }
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }
    }
}