using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using MonadicComponents;
using MonadicComponents.Models;
using MonadicComponents.Filters;
using System.IO;


  [Route("api/v1/Lecture")]
  public class LectureApiController : Controller
  {
    private readonly MailOptions _mailOptions;
    public readonly MonadicComponentsContext _context;
    private IHostingEnvironment env;

    public LectureApiController(MonadicComponentsContext context, IHostingEnvironment env, IOptions<MailOptions> mailOptionsAccessor)
    {
      _context = context;
      _mailOptions = mailOptionsAccessor.Value;
      this.env = env;
    }

    public bool ApiTokenValid => RestrictToUserTypeAttribute.ApiToken != null &&
        HttpContext.Request.Headers["ApiToken"] == RestrictToUserTypeAttribute.ApiToken;

    
    [RestrictToUserType(new string[] {"*"})]
    [HttpGet("{Lecture_id}/Course_Lectures")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public Page<Course> GetCourse_Lectures(int Lecture_id, [FromQuery] int page_index, [FromQuery] int page_size = 25 )
    {
      var session = HttpContext.Get<LoggableEntities>(_context);

      var allowed_sources = ApiTokenValid ? _context.Lecture : _context.Lecture;
      var source = allowed_sources.FirstOrDefault(s => s.Id == Lecture_id);
      var can_create_by_token = ApiTokenValid || true;
      var can_delete_by_token = ApiTokenValid || true || true;
      var can_link_by_token = ApiTokenValid || true;
      var can_view_by_token = ApiTokenValid || true;
      if (source == null || !can_view_by_token) // test
        return Enumerable.Empty<MonadicComponents.Models.Course>() // B
              .AsQueryable()
              .Select(MonadicComponents.Models.Course.FilterViewableAttributes())
              .Select(t => Tuple.Create(t, false))
              .Paginate(can_create_by_token, can_delete_by_token, can_link_by_token, page_index, page_size, MonadicComponents.Models.Course.WithoutImages, item => item , null);
      var allowed_targets = ApiTokenValid ? _context.Course : _context.Course;
      var editable_targets = ApiTokenValid ? _context.Course : (_context.Course);
      var can_edit_by_token = ApiTokenValid || true;
      var items = (from link in _context.Course_Lecture
              where link.LectureId == source.Id
              from target in allowed_targets
              where link.CourseId == target.Id
              select target).OrderBy(i => i.CreatedDate).AsQueryable();
      
      return items
              .Select(MonadicComponents.Models.Course.FilterViewableAttributes())
              .Select(t => Tuple.Create(t, can_edit_by_token && editable_targets.Any(et => et.Id == t.Id)))
              .Paginate(can_create_by_token, can_delete_by_token, can_link_by_token, page_index, page_size, MonadicComponents.Models.Course.WithoutImages, item => item , null);
    }

    [HttpGet("{Lecture_id}/Course_Lectures/{Course_id}")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult /*Course*/ GetCourse_LectureById(int Lecture_id, int Course_id)
    {
      var session = HttpContext.Get<LoggableEntities>(_context);

      var allowed_sources = ApiTokenValid ? _context.Lecture : _context.Lecture;
      var source = allowed_sources.FirstOrDefault(s => s.Id == Lecture_id);
      var can_view_by_token = ApiTokenValid || true;
      if (source == null || !can_view_by_token)
        return NotFound();
      var allowed_targets = ApiTokenValid ? _context.Course : _context.Course;
      var item = (from link in _context.Course_Lecture
              where link.LectureId == source.Id
              from target in allowed_targets
              where link.CourseId == target.Id
              select target).OrderBy(i => i.CreatedDate)
              .Select(MonadicComponents.Models.Course.FilterViewableAttributes())
              .FirstOrDefault(t => t.Id == Course_id);
      if (item == null) return NotFound();
      item = MonadicComponents.Models.Course.WithoutImages(item);
      return Ok(item);
    }

    [RestrictToUserType(new string[] {"*"})]
    [HttpGet("{Lecture_id}/unlinked/Course_Lectures")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public Page<Course> GetUnlinkedCourse_Lectures(int Lecture_id, [FromQuery] int page_index, [FromQuery] int page_size = 25)
    {
      var session = HttpContext.Get<LoggableEntities>(_context);

      var allowed_sources = ApiTokenValid ? _context.Lecture : _context.Lecture;
      var source = allowed_sources.FirstOrDefault(s => s.Id == Lecture_id);
      var can_create_by_token = ApiTokenValid || true;
      var can_delete_by_token = ApiTokenValid || true || true;
      var can_link_by_token = ApiTokenValid || true;
      var can_view_by_token = ApiTokenValid || true;
      if (source == null || !can_view_by_token)
        return Enumerable.Empty<MonadicComponents.Models.Course>()
              .AsQueryable()
              .Select(MonadicComponents.Models.Course.FilterViewableAttributes())
              .Select(t => Tuple.Create(t, false))
              .Paginate(can_create_by_token, can_delete_by_token, can_link_by_token, page_index, page_size, MonadicComponents.Models.Course.WithoutImages, item => item);
      var allowed_targets = ApiTokenValid ? _context.Course : _context.Course;
      var editable_targets = ApiTokenValid ? _context.Course : (_context.Course);
      var can_edit_by_token = ApiTokenValid || true;
      return (from target in allowed_targets
              where !_context.Course_Lecture.Any(link => link.LectureId == source.Id && link.CourseId == target.Id) &&
              true
              select target).OrderBy(i => i.CreatedDate)
              .Select(MonadicComponents.Models.Course.FilterViewableAttributes())
              .Select(t => Tuple.Create(t, can_edit_by_token && editable_targets.Any(et => et.Id == t.Id)))
              .Paginate(can_create_by_token, can_delete_by_token, can_link_by_token, page_index, page_size, MonadicComponents.Models.Course.WithoutImages, item => item);
    }

    bool CanAdd_Lecture_Course_Lectures(Lecture source) {
      return (from link in _context.Course_Lecture
           where link.LectureId == source.Id
           from target in _context.Course
           where link.CourseId == target.Id
           select target).Count() < 1;
    }

    bool CanAdd_Course_Course_Lectures(Course target) {
      return true;
    }

    [RestrictToUserType(new string[] {"*"})]
    [HttpPost("{Lecture_id}/Course_Lectures_Course")]
    public IActionResult /*IEnumerable<Course>*/ CreateNewCourse_Lecture_Course(int Lecture_id)
    {
      var session = HttpContext.Get<LoggableEntities>(_context);

      var allowed_sources = ApiTokenValid ? _context.Lecture : _context.Lecture;
      var source = allowed_sources.FirstOrDefault(s => s.Id == Lecture_id);
      var can_create_by_token = ApiTokenValid || true;
      if (source == null || !can_create_by_token)
        return Unauthorized();
        // throw new Exception("Cannot create item in relation Course_Lectures");
      var can_link_by_token = ApiTokenValid || true;
      if (!CanAdd_Lecture_Course_Lectures(source) || !can_link_by_token)
        return Unauthorized();
        //throw new Exception("Cannot add item to relation Course_Lectures");
      var new_target = new Course() { CreatedDate = DateTime.Now, Id = _context.Course.Max(i => i.Id) + 1 };
      _context.Course.Add(new_target);
      _context.SaveChanges();
      var link = new Course_Lecture() { Id = _context.Course_Lecture.Max(l => l.Id) + 1, LectureId = source.Id, CourseId = new_target.Id };
      _context.Course_Lecture.Add(link);
      _context.SaveChanges();
      return Ok(new Course[] { new_target });
    }

    [RestrictToUserType(new string[] {"*"})]
    [HttpPost("{Lecture_id}/Course_Lectures/{Course_id}")]
    public IActionResult LinkWithCourse_Lecture(int Lecture_id, int Course_id)
    {
      var session = HttpContext.Get<LoggableEntities>(_context);

      var allowed_sources = _context.Lecture;
      var source = allowed_sources.FirstOrDefault(s => s.Id == Lecture_id);
      var allowed_targets = _context.Course;
      var target = allowed_targets.FirstOrDefault(s => s.Id == Course_id);
      var can_edit_source_by_token = ApiTokenValid || true;
      var can_edit_target_by_token = ApiTokenValid || true;
      var can_link_by_token = ApiTokenValid || true;
      if (!CanAdd_Lecture_Course_Lectures(source) || !can_link_by_token || !can_edit_source_by_token || !can_edit_target_by_token)
        return BadRequest();
        // throw new Exception("Cannot add item to relation Course_Lectures");
      if (!CanAdd_Course_Course_Lectures(target))
        return BadRequest();
        // throw new Exception("Cannot add item to relation Course_Lectures");
      var link = new Course_Lecture() { Id = _context.Course_Lecture.Max(i => i.Id) + 1, LectureId = source.Id, CourseId = target.Id };
      _context.Course_Lecture.Add(link);
      _context.SaveChanges();
      return Ok();
    }
    [RestrictToUserType(new string[] {"*"})]
    [HttpDelete("{Lecture_id}/Course_Lectures/{Course_id}")]
    public IActionResult UnlinkFromCourse_Lecture(int Lecture_id, int Course_id)
    {
      var session = HttpContext.Get<LoggableEntities>(_context);

      var allowed_sources = _context.Lecture;
      var source = allowed_sources.FirstOrDefault(s => s.Id == Lecture_id);
      var allowed_targets = _context.Course;
      var target = allowed_targets.FirstOrDefault(s => s.Id == Course_id);
      var link = _context.Course_Lecture.FirstOrDefault(l => l.LectureId == source.Id && l.CourseId == target.Id);

      var can_edit_source_by_token = ApiTokenValid || true;
      var can_edit_target_by_token = ApiTokenValid || true;
      var can_unlink_by_token = ApiTokenValid || true;
      if (!can_unlink_by_token || !can_edit_source_by_token || !can_edit_target_by_token) return Unauthorized(); // throw new Exception("Cannot remove item from relation Course_Lectures");
      _context.Course_Lecture.Remove(link);
      _context.SaveChanges();
      return Ok();
    }
    [RestrictToUserType(new string[] {"*"})]
    [HttpGet("{id}")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult /*ItemWithEditable<Lecture>*/ GetById(int id)
    {
      var session = HttpContext.Get<LoggableEntities>(_context);

      var allowed_items = ApiTokenValid ? _context.Lecture : _context.Lecture;
      var editable_items = ApiTokenValid ? _context.Lecture : _context.Lecture;
      var item_full = allowed_items.FirstOrDefault(e => e.Id == id);
      if (item_full == null) return NotFound();
      var item = MonadicComponents.Models.Lecture.FilterViewableAttributesLocal()(item_full);
      item = MonadicComponents.Models.Lecture.WithoutImages(item);
      return Ok(new ItemWithEditable<Lecture>() {
        Item = item,
        Editable = editable_items.Any(e => e.Id == item.Id) });
    }
    

    [RestrictToUserType(new string[] {"*"})]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult /*Lecture*/ Create()
    {
      var session = HttpContext.Get<LoggableEntities>(_context);

      var can_create_by_token = ApiTokenValid || true;
      if (!can_create_by_token)
        return Unauthorized();
        // throw new Exception("Unauthorized create attempt");
      var item = new Lecture() { CreatedDate = DateTime.Now, Id = _context.Lecture.Max(i => i.Id) + 1 };
      _context.Lecture.Add(MonadicComponents.Models.Lecture.FilterViewableAttributesLocal()(item));
      _context.SaveChanges();
      item = MonadicComponents.Models.Lecture.WithoutImages(item);
      return Ok(item);
    }

    [RestrictToUserType(new string[] {"*"})]
    [HttpPut]
    [ValidateAntiForgeryToken]
    public IActionResult Update([FromBody] Lecture item)
    {
      var session = HttpContext.Get<LoggableEntities>(_context);

      var allowed_items = ApiTokenValid ? _context.Lecture : _context.Lecture;
      if (!allowed_items.Any(i => i.Id == item.Id)) return Unauthorized();
      var new_item = item;
      
      var can_edit_by_token = ApiTokenValid || true;
      if (item == null || !can_edit_by_token)
        return Unauthorized();
        // throw new Exception("Unauthorized edit attempt");
      _context.Update(new_item);
      _context.Entry(new_item).Property(x => x.CreatedDate).IsModified = false;
      _context.SaveChanges();
      return Ok();
    }

    [RestrictToUserType(new string[] {"*"})]
    [HttpDelete("{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
      var session = HttpContext.Get<LoggableEntities>(_context);

      var allowed_items = ApiTokenValid ? _context.Lecture : _context.Lecture;
      var item = _context.Lecture.FirstOrDefault(e => e.Id == id);
      var can_delete_by_token = ApiTokenValid || true;
      if (item == null || !can_delete_by_token)
        return Unauthorized();
        // throw new Exception("Unauthorized delete attempt");
      
      if (!allowed_items.Any(a => a.Id == item.Id)) return Unauthorized(); // throw new Exception("Unauthorized delete attempt");
      
      

      _context.Lecture.Remove(item);
      _context.SaveChanges();
      return Ok();
    }


    [RestrictToUserType(new string[] {"*"})]
    [HttpGet]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public Page<Lecture> GetAll([FromQuery] int page_index, [FromQuery] int page_size = 25 )
    {
      var session = HttpContext.Get<LoggableEntities>(_context);

      var allowed_items = ApiTokenValid ? _context.Lecture : _context.Lecture;
      var editable_items = ApiTokenValid ? _context.Lecture : _context.Lecture;
      var can_edit_by_token = ApiTokenValid || true;
      var can_create_by_token = ApiTokenValid || true;
      var can_delete_by_token = ApiTokenValid || true;
      var items = allowed_items.OrderBy(i => i.CreatedDate).AsQueryable();
      
      return items
        .Select(MonadicComponents.Models.Lecture.FilterViewableAttributes())
        .Select(s => Tuple.Create(s, can_edit_by_token && editable_items.Any(es => es.Id == s.Id)))
        .Paginate(can_create_by_token, can_delete_by_token, false, page_index, page_size, MonadicComponents.Models.Lecture.WithoutImages, item => item , null );
    }

    


    
  }

  