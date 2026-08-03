using System.ComponentModel.DataAnnotations;

namespace API.Palantir.Models.Discipline
{
    public class NoteDisciplineRequest
    {
        [Required(ErrorMessage = "_id는 필수입니다")]
        public string _id { get; set; } = default!;
        [Required(ErrorMessage = "note는 필수입니다")]
        public string note { get; set; } = default!;
    }
}
