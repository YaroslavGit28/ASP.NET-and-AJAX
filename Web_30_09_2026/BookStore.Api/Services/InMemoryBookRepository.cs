namespace BookStore.Api.Services;

public class InMemoryBookRepository : IBookRepository
{
    private readonly List<Book> _books = new()
    {
        new Book
        {
            Id = 1,
            Title = "Clean Code",
            Author = "Robert C. Martin",
            Price = 30
        },
        new Book
        {
            Id = 2,
            Title = "The Pragmatic Programmer",
            Author = "Andrew Hunt",
            Price = 40
        }
    };

    public IEnumerable<Book> GetAll()
    {
        return _books;
    }

    public Book? GetById(int id)
    {
        return _books.FirstOrDefault(x => x.Id == id);
    }

    public void Add(Book book)
    {
        book.Id = _books.Count + 1;
        _books.Add(book);
    }
}