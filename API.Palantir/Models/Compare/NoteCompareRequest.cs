using System.ComponentModel.DataAnnotations;

namespace API.Palantir.Models.Compare
{
    public class NoteCompareRequest
    {
        [Required(ErrorMessage = "_id는 필수입니다")]
        public string _id { get; set; } = default!;
        [Required(ErrorMessage = "noteOld는 필수입니다")]
        public string noteOld { get; set; } = default!;
        [Required(ErrorMessage = "noteNew는 필수입니다")]
        public string noteNew { get; set; } = default!;
    }
}
