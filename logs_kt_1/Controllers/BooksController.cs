using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using logs_kt_1.Data;
using logs_kt_1.Models;
using System.Diagnostics;

namespace logs_kt_1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly ApplicationDbConext _context;

        public BooksController(ApplicationDbConext conext)
        {
            _context = conext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Book>>> GetBooks()
        {
            Trace.WriteLine("Начало операции GetBooks.");

            var books = await _context.Books.ToListAsync();

            if (books.Count == 0)
            {
                Trace.TraceInformation("Список книг пуст.");
                Trace.WriteLine("Конец операции GetBooks.");
                return Ok(books);
            }

            Trace.TraceInformation($"Получен список книг. Количество: {books.Count}");
            Trace.WriteLine("Конец операции GetBooks.");
            return Ok(books);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Book>> GetBook(int id)
        {
            Trace.WriteLine("Начало операции GetBook.");

            var book = await _context.Books.FindAsync(id);

            if (book == null)
            {
                Trace.TraceError($"Книга с Id = {id} не найдена.");
                Trace.WriteLine("Конец операции GetBook.");
                return NotFound();
            }

            Trace.TraceInformation($"Книга с Id = {id} успешно найдена.");
            Trace.WriteLine("Конец операции GetBook.");
            return Ok(book);
        }

        [HttpPost]
        public async Task<ActionResult<Book>> PostBook(Book book)
        {
            Trace.WriteLine("Начало операции PostBook.");

            if (string.IsNullOrWhiteSpace(book.Title))
            {
                Trace.TraceWarning("Попытка добавить книгу с пустым названием. Операция не выполнена.");
                Trace.WriteLine("Конец операции PostBook.");
                return BadRequest("Название книги не может быть пустым.");
            }

            if (string.IsNullOrWhiteSpace(book.Author))
            {
                Trace.TraceWarning("Попытка добавить книгу с пустым автором. Операция не выполнена.");
                Trace.WriteLine("Конец операции PostBook.");
                return BadRequest("Автор книги не может быть пустым.");
            }

            if (book.Year < 0)
            {
                Trace.TraceError($"Неверный год {book.Year} для книги \"{book.Title}\".");
                Trace.WriteLine("Конец операции PostBook.");
                return BadRequest("Год не может быть отрицательным.");
            }

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            var count = await _context.Books.CountAsync();
            Trace.TraceInformation($"Книга \"{book.Title}\" успешно добавлена.");
            Trace.TraceInformation($"Количество книг после добавления: {count}");
            Trace.WriteLine("Конец операции PostBook.");

            return CreatedAtAction(nameof(GetBook), new { id = book.Id }, book);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutBook(int id, Book book)
        {
            Trace.WriteLine("Начало операции PutBook.");

            if (id != book.Id)
            {
                Trace.TraceWarning("Несоответствие Id в маршруте и объекте книги. Операция не выполнена.");
                Trace.WriteLine("Конец операции PutBook.");
                return BadRequest();
            }

            if (string.IsNullOrWhiteSpace(book.Title))
            {
                Trace.TraceWarning("Попытка обновить книгу с пустым названием. Операция не выполнена.");
                Trace.WriteLine("Конец операции PutBook.");
                return BadRequest("Название книги не может быть пустым.");
            }

            if (string.IsNullOrWhiteSpace(book.Author))
            {
                Trace.TraceWarning("Попытка обновить книгу с пустым автором. Операция не выполнена.");
                Trace.WriteLine("Конец операции PutBook.");
                return BadRequest("Автор книги не может быть пустым.");
            }

            if (book.Year < 0)
            {
                Trace.TraceError($"Неверный год {book.Year} для книги \"{book.Title}\".");
                Trace.WriteLine("Конец операции PutBook.");
                return BadRequest("Год не может быть отрицательным.");
            }

            _context.Entry(book).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                Trace.TraceInformation($"Книга с Id = {id} успешно обновлена.");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Books.Any(e => e.Id == id))
                {
                    Trace.TraceError($"Книга с Id = {id} не найдена. Обновление невозможно.");
                    Trace.WriteLine("Конец операции PutBook.");
                    return NotFound();
                }

                Trace.TraceError($"Критическая ошибка при обновлении книги с Id = {id}."); //"Trace" не содержит определение для "TraceEvent".
                Trace.WriteLine("Конец операции PutBook.");
                throw;
            }

            Trace.WriteLine("Конец операции PutBook.");
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            Trace.WriteLine("Начало операции DeleteBook.");

            var book = await _context.Books.FindAsync(id);

            if (book == null)
            {
                Trace.TraceError($"Книга с Id = {id} не найдена. Удаление невозможно.");
                Trace.WriteLine("Конец операции DeleteBook.");
                return NotFound();
            }

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            var count = await _context.Books.CountAsync();
            Trace.TraceInformation($"Книга с Id = {id} успешно удалена.");
            Trace.TraceInformation($"Количество книг после удаления: {count}");
            Trace.WriteLine("Конец операции DeleteBook.");

            return NoContent();
        }
    }
}