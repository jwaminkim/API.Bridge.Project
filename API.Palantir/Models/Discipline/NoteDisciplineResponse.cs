using System.ComponentModel.DataAnnotations;

namespace API.Palantir.Models.Discipline
{
    public class NoteDisciplineResponse
    {
        [Required(ErrorMessage = "_id는 필수입니다")]
        public string _id { get; set; } = default!;
        [Required(ErrorMessage = "discipline는 필수입니다")]
        public string discipline { get; set; } = default!;
    }
}
