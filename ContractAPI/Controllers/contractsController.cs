using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
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
    public class contractsController : ApiController
    {
        private B110_CONTRACTEntities db = new B110_CONTRACTEntities();

        // GET: api/contracts
        public IQueryable<contract> Getcontract()
        {
            return db.contract;
        }  

        // GET: api/contracts/5
        [ResponseType(typeof(contract))]
        public async Task<IHttpActionResult> Getcontract(string id)
        {
            contract contract = await db.contract.FindAsync(id);
            if (contract == null)
            {
                return NotFound();
            }

            return Ok(contract);
        }

        // PUT: api/contracts/5
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> Putcontract(string id, contract contract)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != contract.contract_id)
            {
                Debug.WriteLine("id: "+ id);
                return BadRequest();
            }

            db.Entry(contract).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!contractExists(id))
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

        // POST: api/contracts
        [ResponseType(typeof(contract))]
        public async Task<IHttpActionResult> Postcontract(contract contract)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.contract.Add(contract);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (contractExists(contract.contract_id))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtRoute("DefaultApi", new { id = contract.contract_id }, contract);
        }

        // DELETE: api/contracts/5
        [ResponseType(typeof(contract))]
        public async Task<IHttpActionResult> Deletecontract(string id)
        {
            contract contract = await db.contract.FindAsync(id);
            if (contract == null)
            {
                return NotFound();
            }

            db.contract.Remove(contract);
            await db.SaveChangesAsync();

            return Ok(contract);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool contractExists(string id)
        {
            return db.contract.Count(e => e.contract_id == id) > 0;
        }
    }
}