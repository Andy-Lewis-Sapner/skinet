using Core.Entities;
using Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace Infrastructure.Services;

public class CouponService(IConfiguration config) : ICouponService
{
    public async Task<AppCoupon?> GetCouponFromPromoCode(string code)
    {
        StripeConfiguration.ApiKey = config["StripeSettings:SecretKey"];
        PromotionCodeService service = new();
        PromotionCodeListOptions options = new()
        {
            Code = code.ToUpper(),
            Limit = 1
        };

        StripeList<PromotionCode> list = await service.ListAsync(options);
        PromotionCode? promoCode = list.FirstOrDefault();
        if (promoCode == null) return null;

        Coupon coupon = promoCode.Coupon;
        return new AppCoupon
        {
            Name = coupon.Name,
            AmountOff = coupon.AmountOff,
            PercentOff = coupon.PercentOff,
            PromotionCode = promoCode.Code,
            CouponId = coupon.Id
        };
    }
}
