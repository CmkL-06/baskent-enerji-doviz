using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Business.Tools;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.Site.Form;
using BaskentEnerji.Entity.Modals.RequestModals.Site.Form;
using BaskentEnerji.Entity.Modals.ResponseModals.Form;
using System.ComponentModel.DataAnnotations;

namespace BaskentEnerji.API.Controllers.Site
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class FormController : ControllerBase
    {
        private readonly BaskentEnerjiDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ValidationService _validationService;

        public FormController(BaskentEnerjiDbContext dbContext, IMapper mapper, ValidationService validationService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _validationService = validationService;
        }

        [HttpPost]
        public async Task<IActionResult> PostForm([FromBody] rm_form_save data)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var dbCheckForm = await _dbContext.Forms.FirstOrDefaultAsync(x => x.Id == data.Id);
            if (dbCheckForm != null)
            {
                dbCheckForm.FormStructure = data.Fields ?? string.Empty;
                dbCheckForm.CustomName = data.CustomName ?? string.Empty;
                dbCheckForm.Name = data.Name ?? string.Empty;
                dbCheckForm.SubmitMessage = data.SubmitMessage ?? string.Empty;
                dbCheckForm.Title = data.Title ?? string.Empty;
                dbCheckForm.SubTitle = data.SubTitle ?? string.Empty;
                dbCheckForm.Footer = data.Footer ?? string.Empty;
                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "Form updated successfully", formId = dbCheckForm.Id });
            }

            var newForm = new Form
            {
                Id = data.Id ?? Guid.NewGuid(),
                FormStructure = data.Fields ?? string.Empty,
                CustomName = data.CustomName ?? string.Empty,
                Name = data.Name ?? string.Empty,
                SubmitMessage = data.SubmitMessage ?? string.Empty,
                SubTitle = data.SubTitle ?? string.Empty,
                Footer = data.Footer ?? string.Empty,
                Title = data.Title ?? string.Empty
            };

            _dbContext.Forms.Add(newForm);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Form created successfully", formId = newForm.Id });
        }

        [HttpGet("Id")]
        public async Task<ActionResult<Form>> GetForm([FromQuery]Guid id)
        {
            var form = await _dbContext.Forms.FindAsync(id);
            if (form == null)
            {
                return NotFound(new { message = "Form not found" });
            }
            return form;
        }

        [HttpGet]
        public async Task<ActionResult<List<Form>>> GetForms()
        {
            return await _dbContext.Forms.ToListAsync();
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitForm([FromBody] rm_form_submit data)
        {
            if (data == null)
            {
                return BadRequest(new { message = "Invalid form data" });
            }

            var dbCheckSubmit = await _dbContext.Form_Submits.FirstOrDefaultAsync(x => x.Id == data.Id);
            if (dbCheckSubmit != null)
            {
                dbCheckSubmit.data = data.data ?? string.Empty;
                dbCheckSubmit.IsActive = data.IsActive;
                dbCheckSubmit.Notes = data.Notes ?? string.Empty;
                dbCheckSubmit.FormId = data.FormId;
            }
            else
            {
                var formSubmit = new Form_Submit
                {
                    FormId = data.FormId,
                    data = data.data ?? string.Empty,
                    IsActive = true,
                    IpAdress = tools_string.GetIpAddress(HttpContext)
                };
                _dbContext.Form_Submits.Add(formSubmit);
            }

            await _dbContext.SaveChangesAsync();
            return Ok(new { message = "Form submission saved successfully" });
        }

        [HttpGet("submits")]
        public async Task<ActionResult<List<rsp_form_submit>>> GetFormSubmits()
        {
            var submits = await _dbContext.Form_Submits.Include(x => x.Form).ToListAsync();
            return _mapper.Map<List<rsp_form_submit>>(submits);
        }

        [HttpPost("remove")]
        public async Task<IActionResult> RemoveForm([FromBody, Required] Guid id)
        {
            var form = await _dbContext.Forms.FindAsync(id);
            if (form == null)
            {
                return NotFound(new { message = "Form not found" });
            }

            _dbContext.Forms.Remove(form);
            await _dbContext.SaveChangesAsync();
            return Ok(new { message = "Form removed successfully" });
        }

        [HttpGet("check")]
        public async Task<IActionResult> CheckSubmit([FromQuery, Required] Guid formId)
        {
            string visitorIp = tools_string.GetIpAddress(HttpContext);
            bool isSubmitted = await _dbContext.Form_Submits.AnyAsync(x => x.FormId == formId && x.IpAdress == visitorIp);

            return Ok(new { isSubmitted });
        }
    }
}
