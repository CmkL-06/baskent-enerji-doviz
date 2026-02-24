using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Entities.Site.Form
{
    public class Form : BaseEntity
    {


        [MaxLength(255)]
        public string Name { get; set; }

       
        [Column(TypeName = "NVARCHAR(MAX)")] 
        public string FormStructure { get; set; }

        [MaxLength(100)]
        public string CustomName { get; set; }
        public string Title { get; set; }
        public string SubTitle { get; set; }
        public string Footer { get; set; }
        public string SubmitMessage { get; set; }

    }
}
