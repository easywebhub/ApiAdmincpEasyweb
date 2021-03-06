﻿using ew.application.Entities.Dto;
using ew.application.Helpers;
using ew.application.Managers;
using ew.application.Services;
using ew.common;
using ew.common.Entities;
using ew.common.Helper;
using ew.core;
using ew.core.Repositories;
using ew.core.Users;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ew.application.Entities
{
    public class EwhAccount : EwhEntityBase
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IWebsiteRepository _websiteRepository;
        private readonly Lazy<IWebsiteManager> _websiteManager;
        private IWebsiteManager websiteManager { get { return _websiteManager.Value; } }

        private readonly Lazy<IEwhMapper> _ewhMapper;
        private IEwhMapper ewhMapper { get { return _ewhMapper.Value; } }
        //private readonly AuthService _authService;
        public EwhAccount(Lazy<IWebsiteManager> websiteManager, IWebsiteRepository websiteRepository, IAccountRepository accountRepository, Lazy<IEwhMapper> ewhMapper)
        {
            _websiteRepository = websiteRepository;
            _accountRepository = accountRepository;
            //_entityFactory = entityFactory;
            _websiteManager = websiteManager;
           
            _ewhMapper = ewhMapper;
            //_authService = authService;
        }

        public EwhAccount(Account account, Lazy<IWebsiteManager> websiteManager, IWebsiteRepository websiteRepository, IAccountRepository accountRepository, Lazy<IEwhMapper> ewhMapper): this( websiteManager, websiteRepository, accountRepository, ewhMapper)
        {
            _account = account;
            MapFrom(account);
        }

        public EwhAccount(string accountId, Lazy<IWebsiteManager> websiteManager, IWebsiteRepository websiteRepository, IAccountRepository accountRepository, Lazy<IEwhMapper> ewhMapper) : this( websiteManager, websiteRepository, accountRepository, ewhMapper)
        {
            _account = _accountRepository.Get(accountId);
            MapFrom(_account);
        }



        #region properties
        public string AccountId { get; private set; }
        public string AccountType { get; set; }
        public string Password { get; private set; }
        public string PasswordSaft { get; private set; }
        [Required]
        public string UserName { get; set; }
        public string Status { get; set; }
        public AccountInfo Info { get; set; }
        public List<WebsitesAccessLevelOfAccount> Websites { get; set; }
        #endregion

        #region ext properties
        private Account _account;
        //private List<EwhWebsite> _ewhWebsites { get; set; }
        //public List<EwhWebsite> EwhWebsites
        //{
        //    get
        //    {
        //        if (_ewhWebsites == null) _ewhWebsites = _websiteService.GetListWebsite(this.Websites.Select(x => x.WebsiteId).ToList());
        //        return _ewhWebsites;
        //    }
        //    private set { _ewhWebsites = value; }
        //}
        #endregion

        #region public methods

        public List<EwhWebsite> GetListWebsite()
        {
            return websiteManager.GetListEwhWebsite(this.Websites.Select(x => x.WebsiteId).ToList());
        }

        public bool IsExits()
        {
            if (!string.IsNullOrEmpty(AccountId))
            {
                return true;
            }
            EwhStatus = core.Enums.GlobalStatus.NotFound;
            return false;
        }

        public bool Create(AddAccountDto dto)
        {
            if (!ValidateHelper.Validate(dto, out ValidateResults))
            {
                EwhStatus = core.Enums.GlobalStatus.InvalidData;
                return false;
            }
            ewhMapper.ToEntity(this, dto);
            this.PasswordSaft = StringUtils.CreateSalt(20);
            this.Password = StringUtils.GenerateSaltedHash(dto.Password, this.PasswordSaft);
            return Save();
        }

        public bool Create()
        {
            if (Save())
            {
                AccountId = _account.Id;
                SelfSync();
                return true;
            }
            return false;
        }

        public bool Save()
        {
            if (CheckValidModel() && CheckIsIdentity())
            {
                if (_account == null) _account = new Account();
                _accountRepository.AddOrUpdate(ewhMapper.ToEntity(_account, this));
                AccountId = _account.Id;
                return true;
            }
            return false;
        }

        public bool UpdateInfo(AccountInfo info)
        {
            this.Info = info;
            return Save();
        }

        public bool ChangePassword(string password, string newpassword)
        {
            return true;
        }

        public bool ResetPassword()
        {
            this.PasswordSaft = StringUtils.CreateSalt(20);
            this.Password = StringUtils.GenerateSaltedHash("123456", this.PasswordSaft);
            Save();
            return true;
        }

        public bool RemoveWebsite(EwhWebsite website)
        {
            var webIdentity = this.Websites.FirstOrDefault(x => x.WebsiteId == website.WebsiteId);
            if (webIdentity != null)
            {
                this.Websites.Remove(webIdentity);
                return Save();
            }
            else
            {
                EwhStatus = core.Enums.GlobalStatus.NotFound;
                return false;
            }
        }

        public bool SelfSync()
        {
            var websitesManaged = _websiteRepository.FindAll().Where(x => x.Accounts != null && x.Accounts.Any(y => y.AccountId == this.AccountId)).Select(x => x.Id).ToList();

            if (IsExits())
            {
                EwhLogger.Common.Info("SeftSync start");

                //var newStaggings = this.Stagging.Where(x=>x.Id==)
                var newWebsiteManaged = this.Websites.Where(x => !websitesManaged.Contains(x.WebsiteId)).ToList();
                var removeWebsiteManaged = websitesManaged.Where(x => !(this.Websites.Select(y => y.WebsiteId).ToList()).Contains(x)).ToList();
                foreach (var item in newWebsiteManaged)
                {
                    var website = websiteManager.GetEwhWebsite(item.WebsiteId);
                    if (website != null) website.AddAccount(new AddWebsiteAccountDto() { AccessLevels = item.AccessLevels, AccountId = this.AccountId });
                }
                foreach (var id in removeWebsiteManaged)
                {
                    var website = websiteManager.GetEwhWebsite(id);
                    if (website != null) website.RemoveAccount(this.AccountId);
                }
                EwhLogger.Common.Info("SeftSync end");
            }
            return true;
        }
        #endregion


        #region private methods
        private void MapFrom(Account account)
        {
            if (account == null) return;

            this.AccountId = account.Id;
            this.UserName = account.UserName;
            this.AccountType = account.AccountType;
            this.Status = account.Status;
            this.Info = account.Info ?? new AccountInfo();
            this.Websites = account.Websites ?? new List<WebsitesAccessLevelOfAccount>();
            this.Password = account.Password;
            this.PasswordSaft = account.PasswordSalt;
        }

        private bool CheckValidModel()
        {
            //if (ValidateHelper.Validate(this, out ValidateResults))
            //{
            //    EwhStatus = core.Enums.GlobalStatus.InvalidData;
            //    return false;
            //}
            return true;
        }
        private bool CheckIsIdentity()
        {
            return (IsExits() || CheckValidUserName()) && CheckValidEmail();
        }
        public bool CheckValidUserName()
        {
            if (_accountRepository.IsExitsUserName(this.UserName))
            {
                base.EwhStatus = core.Enums.GlobalStatus.UsernameAlreadyExists;
                return false;
            }
            return true;
        }
        public bool CheckValidEmail()
        {
            if(string.IsNullOrEmpty(this.Info!=null? this.Info.Email: ""))
            {
                base.EwhStatus = core.Enums.GlobalStatus.Invalid;
                return false;
            }
            if (_accountRepository.IsIdentityEmail(this.UserName, this.Info.Email))
            {
                base.EwhStatus = core.Enums.GlobalStatus.Account_EmailAlreadyInUse;
                return false;
            }
            return true;
        }
        private bool CheckValidPassword()
        {
            return true;
        }
        #endregion
    }
}
