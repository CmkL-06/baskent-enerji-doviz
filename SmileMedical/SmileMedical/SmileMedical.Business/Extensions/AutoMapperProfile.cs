using AutoMapper;
using Microsoft.Data.SqlClient;
using SmileMedical.Entity.Entities.Blog;
using SmileMedical.Entity.Entities.Coin;
using SmileMedical.Entity.Entities.ExchangeOffice.Currency;
using SmileMedical.Entity.Entities.ExchangeOffice.Office;
using SmileMedical.Entity.Entities.ExchangeOffice.Party;
using SmileMedical.Entity.Modals.RequestModals.ExchangeService.Party;
using SmileMedical.Entity.Modals.ViewModals.ExchangeOffice.Party;
using SmileMedical.Entity.Entities.Site;
using SmileMedical.Entity.Entities.Site.Form;
using SmileMedical.Entity.Entities.Site.Menu;
using SmileMedical.Entity.Entities.Site.Page;
using SmileMedical.Entity.Entities.Site.Slider;
using SmileMedical.Entity.Entities.User;
using SmileMedical.Entity.Modals.RequestModals.Blog;
using SmileMedical.Entity.Modals.RequestModals.Coin;
using SmileMedical.Entity.Modals.RequestModals.ExchangeService;
using SmileMedical.Entity.Modals.RequestModals.ExchangeService.Office;
using SmileMedical.Entity.Modals.RequestModals.Site.General;
using SmileMedical.Entity.Modals.RequestModals.Site.Language;
using SmileMedical.Entity.Modals.RequestModals.Site.Menu;
using SmileMedical.Entity.Modals.RequestModals.Site.Page;
using SmileMedical.Entity.Modals.RequestModals.Site.Slider;
using SmileMedical.Entity.Modals.RequestModals.Site.Tag;
using SmileMedical.Entity.Modals.RequestModals.User;
using SmileMedical.Entity.Modals.ResponseModals.Blog;
using SmileMedical.Entity.Modals.ResponseModals.Form;
using SmileMedical.Entity.Modals.ResponseModals.Site;
using SmileMedical.Entity.Modals.ResponseModals.Site.General;
using SmileMedical.Entity.Modals.ResponseModals.Site.Menu;
using SmileMedical.Entity.Modals.ViewModals.Coin;
using SmileMedical.Entity.Modals.ViewModals.ExchangeOFfice;
using SmileMedical.Entity.Modals.ViewModals.ExchangeOFfice.Office;
using SmileMedical.Entity.Modals.ViewModals.Menu;
using SmileMedical.Entity.Modals.ViewModals.Slider;
using SmileMedical.Entity.Modals.ViewModals.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmileMedical.Business.Extensions
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {

            CreateMap<Vault, vm_vaultsummary>()
                .ForMember(dest => dest.VaultName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.OfficeName, opt => opt.MapFrom(src => src.Office.OfficeName));

            CreateMap<VaultBalance, vm_vaultbalance>()
                .ForMember(dest => dest.CurrencyCode, opt => opt.MapFrom(src => src.Currency.CurrencyCode))
                .ForMember(dest => dest.CurrencyName, opt => opt.MapFrom(src => src.Currency.CurrencyName))
                .ForMember(dest => dest.CurrencyId, opt => opt.MapFrom(src => src.Currency.Id));

            CreateMap<ExchangeRate, vm_exchangerate>()
                .ForMember(dest => dest.OfficeId, opt => opt.MapFrom(src => src.OfficeId))
                .ForMember(dest => dest.OfficeName, opt => opt.MapFrom(src => src.Office.OfficeName))
                .ForMember(dest => dest.SourceCurrencyCode, opt => opt.MapFrom(src => src.SourceCurrency.CurrencyCode))
                .ForMember(dest => dest.TargetCurrencyCode, opt => opt.MapFrom(src => src.TargetCurrency.CurrencyCode))
                .ForMember(dest => dest.SourceCurrencyName, opt => opt.MapFrom(src => src.SourceCurrency.CurrencyName))
                .ForMember(dest => dest.TargetCurrencyName, opt => opt.MapFrom(src => src.TargetCurrency.CurrencyName));

            CreateMap<Transaction, vm_transaction>()
                .ForMember(dest => dest.VaultName, opt => opt.MapFrom(src => src.Vault.Name))
                .ForMember(dest => dest.OfficeId, opt => opt.MapFrom(src => src.Vault.OfficeId))
                .ForMember(dest => dest.OfficeName, opt => opt.MapFrom(src => src.Vault.Office.OfficeName))
                .ForMember(dest => dest.Details, opt => opt.MapFrom(src => src.Details))
                .ForMember(dest => dest.DeletedBy, opt => opt.Ignore()); // Will be handled manually in service
            
            CreateMap<TransactionDetail, vm_transactiondetail>()
                .ForMember(dest => dest.CurrencyCode, opt => opt.MapFrom(src => src.Currency.CurrencyCode))
                .ForMember(dest => dest.CurrencyName, opt => opt.MapFrom(src => src.Currency.CurrencyName))
                .ForMember(dest => dest.CurrencySymbol, opt => opt.MapFrom(src => src.Currency.CurrencySymbol))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Transaction.Status))
                .ForMember(dest => dest.TransactionDate, opt => opt.MapFrom(src => src.Transaction.TransactionDate))
                .ForMember(dest => dest.UserFirstName, opt => opt.MapFrom(src => src.Transaction.User.Firstname))
                .ForMember(dest => dest.UserLastName, opt => opt.MapFrom(src => src.Transaction.User.Lastname))
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Transaction.User.Username));

            CreateMap<rm_savevault, Vault>();
            CreateMap<rm_savevault, Vault>().ReverseMap();
            CreateMap<rm_saveoffice, Office>();
         

            CreateMap<rm_settings_save, Site_Settings>();
            CreateMap<Site_Settings, vm_settings >();
            CreateMap<Site_Settings, vm_settings >().ReverseMap();

            CreateMap<rm_user_update, User>();
            CreateMap<vm_user, User>();
            CreateMap<vm_user, User>().ReverseMap()
                .ForMember(dest => dest.Rank, opt => opt.MapFrom(src => src.Rank.ToString()));
              
            CreateMap<vm_user_simple, User>();
            CreateMap<vm_user_simple, User>().ReverseMap();

            CreateMap<vm_coin, Coin>();
            CreateMap<vm_coin, Coin>().ReverseMap();

            CreateMap<rm_savecoin_admin, Coin>();
            CreateMap<rm_savecoin_admin, Coin>().ReverseMap();

            CreateMap<rm_coin_get, Coin>().ReverseMap();

            CreateMap<vm_coin_user, Coin_User>();

           
            CreateMap<rm_savefavoritecoin, Coin_User_Favorite>().ReverseMap();

          

            CreateMap<vm_coin_user_table, Coin_User_Table>();

            CreateMap<vm_coin_user, Coin_User>().ReverseMap();
            CreateMap<vm_coin_user_table, Coin_User_Table>().ReverseMap();
            CreateMap<rm_savecoin, Coin_User>();

            CreateMap<rm_savecategory, Blog_Category>();
            CreateMap<rsp_category, Blog_Category>();
            CreateMap<rsp_category, Blog_Category>().ReverseMap();
            CreateMap<rsp_category_name, Blog_Category>();
            CreateMap<rsp_category_name, Blog_Category>().ReverseMap();
            //  CreateMap<rsp_category_name, Blog_Article_Category>();
            CreateMap<rsp_category_name, Blog_Article_Category>().ReverseMap()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Category.Name))
                .ForMember(dest => dest.SeoLink, opt => opt.MapFrom(src => src.Category.SeoLink))
                .ForMember(dest => dest.SeoTitle, opt => opt.MapFrom(src => src.Category.SeoTitle));


          //  CreateMap<rsp_category_guest, Blog_Category>();
              //  .ForMember(dest => dest.ParentCategory.Name, opt => opt.MapFrom(src => src.ParentCategoryName));

            CreateMap<Blog_Article, rsp_article>();
            CreateMap<Blog_Article, rm_savearticle>().ReverseMap();
            CreateMap<Blog_Article, rsp_article_guest>()
                .ForMember(dest => dest.Author, opt => opt.MapFrom(src => src.Author.Firstname + " " + src.Author.Lastname));
            CreateMap<Blog_Article_Comment, rsp_article_comment>();
            CreateMap<Blog_Article_Comment, rm_article_comment>().ReverseMap();


          
            CreateMap<Blog_Article_Category, rsp_article_guest>().ReverseMap();




            CreateMap<Tag, rm_savetag>().ReverseMap();
            CreateMap<Tag, rsp_tag_guest>().ReverseMap();
            CreateMap<Blog_Article, rm_savearticle>();
            CreateMap<Blog_Category, rsp_category_guest>()
                .ForMember(dest => dest.Language, opt => opt.MapFrom(src => src.Language.LanguageName))
                .ForMember(dest => dest.ParentCategoryName, opt => opt.MapFrom(src => src.ParentCategory.Name));
         

            CreateMap<Blog_Category, rsp_category_name>();
            CreateMap<Menu, vm_menu>()
                .ForMember(dest => dest.LanguageName, opt => opt.MapFrom(src => src.Language.LanguageName))
                .ForMember(dest => dest.ParentMenuName, opt => opt.MapFrom(src => src.ParentMenu.Name));
            CreateMap<Menu, vm_menu_guest>()
                .ForMember(dest => dest.LanguageId, opt => opt.MapFrom(src => src.LanguageId))
                .ForMember(dest => dest.LanguageCode, opt => opt.MapFrom(src => src.Language != null ? src.Language.LanguageCode : ""))
                .ForMember(dest => dest.LanguageName, opt => opt.MapFrom(src => src.Language != null ? src.Language.LanguageName : ""));
            CreateMap<Menu, rm_menu_save>().ReverseMap();
            CreateMap<Menu_Item, rm_menuitem_save>().ReverseMap();
            CreateMap<Menu_Item, vm_menu_item>().ReverseMap();
            CreateMap<Menu_Item, vm_menu_item_guest>().ReverseMap();

            CreateMap<Slider, rm_saveslider>().ReverseMap();
            CreateMap<Slider_Item, rm_saveslideritem>().ReverseMap();
            CreateMap<Slider, vm_slider>().ReverseMap();
            CreateMap<Slider, vm_slider_guest>().ReverseMap();
            CreateMap<Slider_Item, vm_slideritem>().ReverseMap();
            CreateMap<Slider_Item, vm_slideritem>();
            CreateMap<Slider_Item, vm_slideritem_guest>().ReverseMap();
            CreateMap<Slider_Item, vm_slideritem_guest>();

            // Page mappings removed - using new structure


            CreateMap<rm_savelanguage, Language>();

            CreateMap<Form_Submit, rsp_form_submit>()
                .ForMember(dest => dest.FormName, opt => opt.MapFrom(src => src.Form.Name));


            CreateMap<rm_savecoin_admin, Coin>();
            CreateMap<rm_addpair, Coin_Pair>();

            CreateMap<rm_saveexchangerate, ExchangeRate>();
            CreateMap<Currency, vm_currency>();
            
            CreateMap<User_Office, vm_useroffice>();
            CreateMap<Office, vm_office>();
            CreateMap<User, SmileMedical.Entity.Modals.ViewModals.User.vm_user>();
            
            // Ghost Party mappings
            CreateMap<GhostPartyAccount, vm_ghostpartyaccount>()
                .ForMember(dest => dest.PartyName, opt => opt.MapFrom(src => src.Party.Name))
                .ForMember(dest => dest.PartyCode, opt => opt.MapFrom(src => src.Party.PartyCode))
                .ForMember(dest => dest.OfficeName, opt => opt.MapFrom(src => src.Office.OfficeName))
                .ForMember(dest => dest.CurrencyCode, opt => opt.MapFrom(src => src.Currency.CurrencyCode))
                .ForMember(dest => dest.CurrencyName, opt => opt.MapFrom(src => src.Currency.CurrencyName));
            
            CreateMap<rm_ghostpartyaccount, GhostPartyAccount>();
            CreateMap<rm_ghostpartyentry, GhostPartyAccountEntry>();
            
            CreateMap<GhostPartyAccountEntry, vm_ghostpartyentry>()
                .ForMember(dest => dest.PartyId, opt => opt.MapFrom(src => src.GhostAccount.PartyId))
                .ForMember(dest => dest.PartyName, opt => opt.MapFrom(src => src.GhostAccount.Party.Name))
                .ForMember(dest => dest.OfficeName, opt => opt.MapFrom(src => src.Office.OfficeName))
                .ForMember(dest => dest.CurrencyCode, opt => opt.MapFrom(src => src.Currency.CurrencyCode))
                .ForMember(dest => dest.VaultName, opt => opt.MapFrom(src => src.Vault.Name));

        }


    }
}
