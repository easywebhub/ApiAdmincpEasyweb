﻿using Couchbase.Core;
using Couchbase.Linq;
using Couchbase.Linq.Extensions;
using Couchbase.N1QL;
using ew.core;
using ew.core.Repositories;
using ew.core.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ew.core.Dtos;

namespace ew.infrastructure.Repositories
{
    public class AccountRepository : GenericRepository<Account>, IAccountRepository
    {
        public AccountRepository(IBucket bucket, IBucketContext context) : base(bucket, context)
        {
        }

        public bool AddWebsite(AddWebsiteAccountModel dto)
        {
            if (dto.Website == null || dto.Account == null) return false;
            var account = dto.Account;
            if (account.Websites == null) account.Websites = new List<WebsitesAccessLevelOfAccount>();
            if (account.Websites.Any(x => x.WebsiteId == dto.Website.Id)) account.Websites.Remove(account.Websites.FirstOrDefault(x => x.WebsiteId == dto.Website.Id));
            account.Websites.Add(new WebsitesAccessLevelOfAccount() { WebsiteId = dto.Website.Id, DisplayName = dto.WebsiteDisplayName, AccessLevels = dto.AccessLevels });
            this.AddOrUpdate(account);
            return true;
        }

        public IQueryable<Account> Find(EntityQueryParams queryParams)
        {
            var sql = FindAll();

            return sql;
        }

        public Account GetByUsername(string username)
        {
            return FindAll().Where(x => x.UserName == username).FirstOrDefault();
        }

        public bool IsExitsUserName(string username)
        {
            return this.FindAll().Any(x => x.UserName == username);
        }

        public bool IsExitsEmail(string email)
        {
            return this.FindAll().Any(x => x.Info != null && x.Info.Email == email);
        }

        public bool IsIdentityEmail(string username, string email)
        {
            return this.FindAll().Any(x => x.UserName != username && x.Info != null && x.Info.Email == email);
        }
        
        public bool RemoveWebsite(RemoveWebsiteAccountModel dto)
        {
            if (dto.Website == null || dto.Account == null) return false;
            var account = dto.Account;
            var webAccount = account.Websites != null ? account.Websites.FirstOrDefault(x => x.WebsiteId == dto.Website.Id) : null;
            if (webAccount == null) return false;
            account.Websites.Remove(webAccount);
            this.AddOrUpdate(account);
            return true;
        }
    }
}
