using Asp.Versioning;
using BookStore.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.Api.Controllers;

[ApiController]
[ApiVersion(1.0, Deprecated = true)]
[Route("api/v{version:apiVersion}/books")]
public class BooksController : ControllerBase
{
    private readonly IBookRepository _repository;

    public BooksController(IBookRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public IActionResult GetBooks()
    {
        return Ok(_repository.GetAll());
    }

    [HttpGet("{id:int}")]
    public IActionResult GetBook(int id)
    {
        var book = _repository.GetById(id);

        if (book == null)
        {
            return NotFound();
        }

        return Ok(book);
    }
}