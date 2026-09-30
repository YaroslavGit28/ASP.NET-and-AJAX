namespace BookStore.Api.Services;

public interface IBookRepository
{
    IEnumerable<Book> GetAll();

    Book? GetById(int id);

    void Add(Book book);
}