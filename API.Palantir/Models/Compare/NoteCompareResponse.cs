using System.ComponentModel.DataAnnotations;

namespace API.Palantir.Models.Compare
{
    public class NoteCompareResponse
    {
        [Required(ErrorMessage = "_id는 필수입니다")]
        public string _id { get; set; } = default!;
        [Required(ErrorMessage = "result는 필수입니다")]
        public string result { get; set; } = default!;
    }
}
