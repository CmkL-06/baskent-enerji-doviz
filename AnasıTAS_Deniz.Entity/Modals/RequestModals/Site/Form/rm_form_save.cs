using System;
using System.ComponentModel.DataAnnotations;

namespace AnasıTAS_Deniz.Entity.Modals.RequestModals.Site.Form
{
    public class rm_form_save
    {
        public Guid? Id { get; set; }

      
        public string Name { get; set; }

       
        public string Fields { get; set; }

        public string? CustomName { get; set; }
        public string Title { get; set; }
        public string SubTitle { get; set; }
        public string Footer { get; set; }
        public string SubmitMessage { get; set; }
    }
}