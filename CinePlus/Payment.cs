using System.ComponentModel.DataAnnotations;

namespace CinePlus.Models
{
    public class Allpayments
    {

        [Required(ErrorMessage = "Please select a payment type.")]
        public string SelectedPaymentType { get; set; }

        public Payment Payment { get; set; }
        public Upi Upi { get; set; }
        public Card Card { get; set; }

        public Allpayments()
        {
            Payment = new Payment();
            Upi = new Upi();
            Card = new Card();
        }
    }
}