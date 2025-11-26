using System.ComponentModel;

namespace CreditCardsSystem.Domain.Enums;

public enum DeliveryStatus
{
    // Branch Statuses

    [Description("[{'k':'en-us', 'v':'Under delivery processing to branch'},"
                + "{'k':'ar-kw', 'v':'جاري التسليم للفرع'}]")]
    BRANCH_UNDER_DELIVERY_PROCESSING = 0,

    [Description("[{'k':'en-us', 'v':'Sent to branch'},"
                + "{'k':'ar-kw', 'v':'تم الإرسال للفرع'}]")]
    BRANCH_SENT = 1,

    [Description("[{'k':'en-us', 'v':'Received by branch'},"
                + "{'k':'ar-kw', 'v':'تم التسليم للفرع'}]")]
    BRANCH_RECEIVED = 2,

    [Description("[{'k':'en-us', 'v':'Delivered to customer by branch'},"
                + "{'k':'ar-kw', 'v':'تم التسليم للعميل بواسطة الفرع'}]")]
    BRANCH_DELIVERED_TO_CUSTOMER = 3,

    [Description("[{'k':'en-us', 'v':'Returned to BCD by branch'},"
                + "{'k':'ar-kw', 'v':'تم إرجاع البطاقة من الفرع لإدارة البطاقات'}]")]
    BRANCH_RETURNED_TO_BCD = 4,

    [Description("[{'k':'en-us', 'v':'Shredded'},"
                + "{'k':'ar-kw', 'v':'تم إتلاف البطاقة'}]")]
    BRANCH_SHREDDED = 5,


    // Courier Statuses

    [Description("[{'k':'en-us', 'v':'Under delivery processing to courier'},"
                + "{'k':'ar-kw', 'v':'جاري العمل لتسليم البطاقة لشركة التوصيل'}]")]
    COURIER_UNDER_DELIVERY_PROCESSING = 10,

    [Description("[{'k':'en-us', 'v':'Sent to courier'},"
                + "{'k':'ar-kw', 'v':'أرسلت لشركة التوصيل'}]")]
    COURIER_SENT = 11,

    [Description("[{'k':'en-us', 'v':'Delivered to customer by courier'},"
                + "{'k':'ar-kw', 'v':'سلمت للعميل بواسطة الشركة'}]")]
    COURIER_DELIVERED_TO_CUSTOMER = 13,

    [Description("[{'k':'en-us', 'v':'Returned to BCD by courier'},"
                + "{'k':'ar-kw', 'v':'أرجعت لإدارة البطاقات من شركة التوصيل'}]")]
    COURIER_RETURNED_TO_BCD = 14,

    [Description("[{'k':'en-us', 'v':'Returned by courier to BCD by customer request'},"
                + "{'k':'ar-kw', 'v':'أرجعت لإدارة البطاقات من شركة التوصيل بطلب من العميل'}]")]
    COURIER_RETURNED_BY_CUSTOMER_REQUEST = 16
}
