using Asp.Versioning;
using BookStore.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.Api.Controllers;

[ApiController]
[ApiVersion(2.0)]
[Route("api/v{version:apiVersion}/books")]
public class BooksV2Controller : ControllerBase
{
    private readonly IBookRepository _repository;

    public BooksV2Controller(IBookRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public IActionResult GetBooks()
    {
        var books = _repository.GetAll();

        return Ok(new
        {
            version = "v2",
            count = books.Count(),
            books
        });
    }

    [HttpGet("{id:int}")]
    public IActionResult GetBook(int id)
    {
        var book = _repository.GetById(id);

        if (book == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            version = "v2",
            book
        });
    }
}