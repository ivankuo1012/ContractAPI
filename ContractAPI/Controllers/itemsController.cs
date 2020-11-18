using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using ContractAPI.Models;

namespace ContractAPI.Controllers
{
    public class itemsController : ApiController
    {
        private CONTRACTEntities db = new CONTRACTEntities();

        // GET: api/items
        public IQueryable<items> Getitems()
        {
            return db.items;
        }

        // GET: api/items/5
        [ResponseType(typeof(items))]
        public async Task<IHttpActionResult> Getitems(int id)
        {
            items items = await db.items.FindAsync(id);
            if (items == null)
            {
                return NotFound();
            }

            return Ok(items);
        }

        // PUT: api/items/5
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> Putitems(int id, items items)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != items.item_id)
            {
                return BadRequest();
            }

            db.Entry(items).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!itemsExists(id))
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

        // POST: api/items
        [ResponseType(typeof(items))]
        public async Task<IHttpActionResult> Postitems(items items)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.items.Add(items);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (itemsExists(items.item_id))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtRoute("DefaultApi", new { id = items.item_id }, items);
        }

        // DELETE: api/items/5
        [ResponseType(typeof(items))]
        public async Task<IHttpActionResult> Deleteitems(int id)
        {
            items items = await db.items.FindAsync(id);
            if (items == null)
            {
                return NotFound();
            }

            db.items.Remove(items);
            await db.SaveChangesAsync();

            return Ok(items);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool itemsExists(int id)
        {
            return db.items.Count(e => e.item_id == id) > 0;
        }
    }
}