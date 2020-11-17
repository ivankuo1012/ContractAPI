﻿using System;
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
    public class contractsController : ApiController
    {
        private CONTRACTEntities db = new CONTRACTEntities();

        public class PageResult<T>
        {
            public int Count { get; set; }
            public int PageIndex { get; set; }
            public int PageSize { get; set; }
            public List<T> Items { get; set; }

        }
       public class ContractItems
        {
            public contracts contractsItem { get; set; }
            public items itemsItem { get; set; }
        }

        [System.Web.Http.HttpGet]
        public PageResult<ContractItems> WhereContract(int? page, int pagesize = 10, string sales = "")
        {
            IQueryable<ContractItems> data = from c in db.contracts
                                         join i in db.items on c.contract_id equals i.contract_id
                                         where c.contract_id == i.contract_id 

                                         select new ContractItems { contractsItem= c, itemsItem=i   };
            int countDetails;
            if (sales != "")
            {
                data = data.Where(x => x.contractsItem.sales == sales);

            }
            
            countDetails = data.Count();


            var result = new PageResult<ContractItems>
            {
                Count = countDetails,
                PageIndex = page ?? 1,
                PageSize = pagesize,
                Items = data.OrderBy(o => o.contractsItem.contract_id).Skip((page - 1 ?? 0) * pagesize).Take(pagesize).ToList()
            };
            return result;
        }
        // GET: api/contracts
        public IQueryable<contracts> Getcontracts()
        {
           // return from v in db.contracts
                  // select v;
           return db.contracts;
        }

        // GET: api/contracts/5
        [ResponseType(typeof(contracts))]
        public async Task<IHttpActionResult> Getcontracts(string id)
        {
            contracts contracts = await db.contracts.FindAsync(id);
            if (contracts == null)
            {
                return NotFound();
            }

            return Ok(contracts);
        }

        // PUT: api/contracts/5
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> Putcontracts(string id, contracts contracts)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != contracts.contract_id)
            {
                return BadRequest();
            }

            db.Entry(contracts).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!contractsExists(id))
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
        [ResponseType(typeof(contracts))]
        public async Task<IHttpActionResult> Postcontracts(contracts contracts)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.contracts.Add(contracts);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (contractsExists(contracts.contract_id))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtRoute("DefaultApi", new { id = contracts.contract_id }, contracts);
        }

        // DELETE: api/contracts/5
        [ResponseType(typeof(contracts))]
        public async Task<IHttpActionResult> Deletecontracts(string id)
        {
            contracts contracts = await db.contracts.FindAsync(id);
            if (contracts == null)
            {
                return NotFound();
            }

            db.contracts.Remove(contracts);
            await db.SaveChangesAsync();

            return Ok(contracts);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool contractsExists(string id)
        {
            return db.contracts.Count(e => e.contract_id == id) > 0;
        }
    }
}