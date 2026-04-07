using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using logs_kt_1.Data;
using logs_kt_1.Models;
using Serilog;
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
                Log.Information("Список книг пуст");
                Trace.TraceInformation("Список книг пуст.");
                Trace.WriteLine("Конец операции GetBooks.");
                return Ok(books);
            }

            Log.Information("Получен список книг. Количество: {Count}", books.Count);
            Trace.TraceInformation("Получен список книг. Количество: " + books.Count);
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
                Log.Error("Книга с Id = {Id} не найдена", id);
                Trace.TraceError("Книга с Id = " + id + " не найдена.");
                Trace.WriteLine("Конец операции GetBook.");
                return NotFound();
            }

            Log.Information("Книга с Id = {Id} успешно найдена. {@Book}", id, book);
            Trace.TraceInformation("Книга с Id = " + id + " успешно найдена.");
            Trace.WriteLine("Конец операции GetBook.");
            return Ok(book);
        }

        [HttpPost]
        public async Task<ActionResult<Book>> PostBook(Book book)
        {
            Trace.WriteLine("Начало операции PostBook.");
            Log.Information("Попытка создать книгу {@Book}", book);

            if (string.IsNullOrWhiteSpace(book.Title))
            {
                Log.Warning("Попытка добавить книгу с пустым названием. Операция не выполнена. {@Book}", book);
                Trace.TraceWarning("Попытка добавить книгу с пустым названием. Операция не выполнена.");
                Trace.WriteLine("Конец операции PostBook.");
                return BadRequest("Название книги не может быть пустым.");
            }

            if (string.IsNullOrWhiteSpace(book.Author))
            {
                Log.Warning("Попытка добавить книгу с пустым автором. Операция не выполнена. {@Book}", book);
                Trace.TraceWarning("Попытка добавить книгу с пустым автором. Операция не выполнена.");
                Trace.WriteLine("Конец операции PostBook.");
                return BadRequest("Автор книги не может быть пустым.");
            }

            if (book.Year < 0)
            {
                Log.Error("Неверный год {Year} для книги {@Book}", book.Year, book);
                Trace.TraceError("Неверный год " + book.Year + " для книги \"" + book.Title + "\".");
                Trace.WriteLine("Конец операции PostBook.");
                return BadRequest("Год не может быть отрицательным.");
            }

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            var count = await _context.Books.CountAsync();
            Log.Information("Книга успешно добавлена {@Book}", book);
            Log.Information("Количество книг после добавления: {Count}", count);
            Trace.TraceInformation("Книга \"" + book.Title + "\" успешно добавлена.");
            Trace.TraceInformation("Количество книг после добавления: " + count);
            Trace.WriteLine("Конец операции PostBook.");

            return CreatedAtAction(nameof(GetBook), new { id = book.Id }, book);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutBook(int id, Book book)
        {
            Trace.WriteLine("Начало операции PutBook.");
            Log.Information("Попытка обновить книгу. RouteId = {RouteId}, {@Book}", id, book);

            if (id != book.Id)
            {
                Log.Warning("Несоответствие Id в маршруте и объекте книги. RouteId = {RouteId}, BodyId = {BodyId}", id, book.Id);
                Trace.TraceWarning("Несоответствие Id в маршруте и объекте книги. Операция не выполнена.");
                Trace.WriteLine("Конец операции PutBook.");
                return BadRequest();
            }

            if (string.IsNullOrWhiteSpace(book.Title))
            {
                Log.Warning("Попытка обновить книгу с пустым названием. {@Book}", book);
                Trace.TraceWarning("Попытка обновить книгу с пустым названием. Операция не выполнена.");
                Trace.WriteLine("Конец операции PutBook.");
                return BadRequest("Название книги не может быть пустым.");
            }

            if (string.IsNullOrWhiteSpace(book.Author))
            {
                Log.Warning("Попытка обновить книгу с пустым автором. {@Book}", book);
                Trace.TraceWarning("Попытка обновить книгу с пустым автором. Операция не выполнена.");
                Trace.WriteLine("Конец операции PutBook.");
                return BadRequest("Автор книги не может быть пустым.");
            }

            if (book.Year < 0)
            {
                Log.Error("Неверный год {Year} для книги {@Book}", book.Year, book);
                Trace.TraceError("Неверный год " + book.Year + " для книги \"" + book.Title + "\".");
                Trace.WriteLine("Конец операции PutBook.");
                return BadRequest("Год не может быть отрицательным.");
            }

            _context.Entry(book).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                Log.Information("Книга с Id = {Id} успешно обновлена. {@Book}", id, book);
                Trace.TraceInformation("Книга с Id = " + id + " успешно обновлена.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!_context.Books.Any(e => e.Id == id))
                {
                    Log.Error("Книга с Id = {Id} не найдена. Обновление невозможно.", id);
                    Trace.TraceError("Книга с Id = " + id + " не найдена. Обновление невозможно.");
                    Trace.WriteLine("Конец операции PutBook.");
                    return NotFound();
                }

                Log.Error(ex, "Критическая ошибка при обновлении книги с Id = {Id}", id);
                Trace.TraceError("Критическая ошибка при обновлении книги с Id = " + id + ".");
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
                Log.Error("Книга с Id = {Id} не найдена. Удаление невозможно.", id);
                Trace.TraceError("Книга с Id = " + id + " не найдена. Удаление невозможно.");
                Trace.WriteLine("Конец операции DeleteBook.");
                return NotFound();
            }

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            var count = await _context.Books.CountAsync();
            Log.Information("Книга с Id = {Id} успешно удалена. {@Book}", id, book);
            Log.Information("Количество книг после удаления: {Count}", count);
            Trace.TraceInformation("Книга с Id = " + id + " успешно удалена.");
            Trace.TraceInformation("Количество книг после удаления: " + count);
            Trace.WriteLine("Конец операции DeleteBook.");

            return NoContent();
        }
    }
}