using System.ComponentModel.DataAnnotations;

namespace Demo1.Models
{
    public class CheckoutViewModel
    {
        public int? BookID { get; set; }
        public int Quantity { get; set; } = 1;

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng")]
        public string Address { get; set; }

        public string PaymentMethod { get; set; } = "Tiền mặt khi nhận hàng (COD)";
    }
}