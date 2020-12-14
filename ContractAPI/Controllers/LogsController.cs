using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using ContractAPI.Models;

namespace ContractAPI.Controllers
{
    public class LogsController : ApiController
    {
        private LOG_SERVICEEntities db = new LOG_SERVICEEntities();

        public class PageResult<T>
        {
            public int Count { get; set; }
            public int PageIndex { get; set; }
            public int PageSize { get; set; }
            public List<T> Items { get; set; }

        }

        // GET: api/Logs
        public PageResult<Logs> GetLogs(int? page, int pagesize = 10, string log_user = null, string log_action = null, string log_detail = null, string start_date = null, string end_date = null)
        {
            int countDetails;
            IQueryable<Logs> data = db.Logs;
            if (log_user != null)
            {
                data = data.Where(d => d.log_user.Equals(log_user));
            }
            if (log_action != null)
            {
                data = data.Where(d => d.log_action.Contains(log_action));
            }
            if (log_detail != null)
            {
                data = data.Where(d => d.log_detail.Contains(log_detail));
            }
            if (start_date != null)
            {
                var date = DateTime.Parse(start_date);
                data = data.Where(d => d.log_time <= date);
            }
            if (end_date != null)
            {
                var date = DateTime.Parse(end_date);
                data = data.Where(d => d.log_time <= date);
            }
            countDetails = data.Count();
            var result = new PageResult<Logs>
            {
                Count = countDetails,
                PageIndex = page ?? 1,
                PageSize = pagesize,
                Items = data.OrderBy(o => o.log_id).Skip((page - 1 ?? 0) * pagesize).Take(pagesize).ToList()
            };
            return result;
        }

        // GET: api/Logs/5
        [ResponseType(typeof(Logs))]
        public async Task<IHttpActionResult> GetLogs(int id)
        {
            Logs logs = await db.Logs.FindAsync(id);
            if (logs == null)
            {
                return NotFound();
            }

            return Ok(logs);
        }

        // PUT: api/Logs/5
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutLogs(int id, Logs logs)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != logs.log_id)
            {
                return BadRequest();
            }

            db.Entry(logs).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LogsExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Logs
        [ResponseType(typeof(Logs))]
        public async Task<IHttpActionResult> PostLogs(Logs logs)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.Logs.Add(logs);
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbEntityValidationException ex)
            {
                var entityError = ex.EntityValidationErrors.SelectMany(x => x.ValidationErrors).Select(x => x.ErrorMessage);
                var getFullMessage = string.Join("; ", entityError);
                var exceptionMessage = string.Concat(ex.Message, "errors are: ", getFullMessage);
                //NLog
                Debug.WriteLine(exceptionMessage);
            }
           

            return CreatedAtRoute("DefaultApi", new { id = logs.log_id }, logs);
        }

        // DELETE: api/Logs/5
        [ResponseType(typeof(Logs))]
        public async Task<IHttpActionResult> DeleteLogs(int id)
        {
            Logs logs = await db.Logs.FindAsync(id);
            if (logs == null)
            {
                return NotFound();
            }

            db.Logs.Remove(logs);
            await db.SaveChangesAsync();

            return Ok(logs);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool LogsExists(int id)
        {
            return db.Logs.Count(e => e.log_id == id) > 0;
        }
    }
}