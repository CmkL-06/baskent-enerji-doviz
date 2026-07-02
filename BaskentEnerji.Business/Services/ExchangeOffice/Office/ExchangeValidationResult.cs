using System.Collections.Generic;
using System.Linq;

namespace BaskentEnerji.Business.Services.ExchangeOffice.Office
{
    public class ExchangeValidationResult
    {
        private readonly List<string> _errors = new List<string>();
        private readonly List<string> _warnings = new List<string>();

        public bool IsValid => !_errors.Any();
        public IReadOnlyList<string> Errors => _errors;
        public IReadOnlyList<string> Warnings => _warnings;

        public void AddError(string error)
        {
            _errors.Add(error);
        }

        public void AddWarning(string warning)
        {
            _warnings.Add(warning);
        }
    }
}
