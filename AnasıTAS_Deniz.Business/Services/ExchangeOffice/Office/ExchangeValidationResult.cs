using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Business.Services.ExchangeOffice.Office
{
    public class ExchangeValidationResult
    {
        private readonly List<string> _errors = new List<string>();

        public bool IsValid => !_errors.Any();
        public IReadOnlyList<string> Errors => _errors;

        public void AddError(string error)
        {
            _errors.Add(error);
        }
    }
}
