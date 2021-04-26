using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        private IUserRepository userRepository;
        private IMapper mapper;
        private LinkGenerator linkGenerator;

        public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
        {
            this.userRepository = userRepository;
            this.mapper = mapper;
            this.linkGenerator = linkGenerator;
        }

        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [HttpHead("{userId}")]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = userRepository.FindById(userId);

            if (user == null)
                return NotFound();

            var userDto = mapper.Map<UserDto>(user);
            return Ok(userDto);
        }

        [HttpPost]
        [Produces("application/json", "application/xml")]
        public IActionResult CreateUser([FromBody] PostUserDto user)
        {
            if (user == null)
                return BadRequest();

            var userEntity = mapper.Map<UserEntity>(user);
            userEntity = userRepository.Insert(userEntity);

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            return CreatedAtRoute(
                nameof(GetUserById),
                new {userId = userEntity.Id},
                userEntity.Id
            );
        }

        [HttpPut("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult UpsertUser([FromRoute] Guid userId, [FromBody] PutUserDto user)
        {
            if (userId == Guid.Empty || user == null)
                return BadRequest();

            var userEntity = mapper.Map(user, new UserEntity(userId));
            userRepository.UpdateOrInsert(userEntity, out var isInserted);

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            if (isInserted)
            {
                return CreatedAtRoute(
                    nameof(GetUserById),
                    new {userId = userEntity.Id},
                    userEntity.Id
                );
            }

            return NoContent();
        }

        [HttpPatch("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<PatchDto> patchDoc)
        {
            if (patchDoc == null)
                return BadRequest();

            var user = userRepository.FindById(userId);
            if (user == null)
                return NotFound();

            var userDto = mapper.Map<PatchDto>(user);
            patchDoc.ApplyTo(userDto, ModelState);

            TryValidateModel(userDto);
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            return NoContent();
        }

        private void TryValidateModel(PatchDto patchDto)
        {
            if (string.IsNullOrEmpty(patchDto.Login) || !patchDto.Login.All(char.IsLetterOrDigit))
                ModelState.AddModelError("Login", "Incorrect login");

            if (string.IsNullOrEmpty(patchDto.FirstName))
                ModelState.AddModelError("FirstName", "Incorrect FirstName");

            if (string.IsNullOrEmpty(patchDto.LastName))
                ModelState.AddModelError("LastName", "Incorrect LastName");
        }

        [HttpDelete("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            var user = userRepository.FindById(userId);
            if (user == null)
                return NotFound();

            userRepository.Delete(userId);

            return NoContent();
        }

        [HttpGet(Name = nameof(GetUsers))]
        [Produces("application/json", "application/xml")]
        public IActionResult GetUsers(int? pageNumber, int? pageSize)
        {
            var number = pageNumber.HasValue && pageNumber.Value > 1 ? pageNumber.Value : 1;
            var size = GetNewPageSize(pageSize);

            var pageList = userRepository.GetPage(number, size);
            var users = mapper.Map<IEnumerable<UserDto>>(pageList);

            var previousPageNumber = pageList.HasPrevious ? number - 1 : (int?) null;
            var nextPageNumber = pageList.HasNext ? number + 1 : (int?) null;

            var paginationHeader = new
            {
                previousPageLink = previousPageNumber != null ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { previousPageNumber, pageSize}) : null,
                nextPageLink = linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { nextPageNumber, pageSize}),
                totalCount = pageList.TotalCount,
                pageSize = size,
                currentPage = number,
                totalPages = pageList.TotalPages
            };

            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
            return Ok(users);
        }

        private int GetNewPageSize(int? oldSize)
        {
            if (!oldSize.HasValue)
                return 10;

            return oldSize.Value < 1 ? 1 : Math.Min(20, oldSize.Value);
        }

        [HttpOptions]
        [Produces("application/json", "application/xml")]
        public IActionResult GetOptions()
        {
            Response.Headers.Add("Allow", new []{"POST", "GET", "OPTIONS"});

            return Ok();
        }
    }
}