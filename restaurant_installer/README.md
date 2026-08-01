# LiteRestaurant — مثبّت Windows 11 (جهاز واحد للمطاعم)

مثبّت يجهّز **خادم المطاعم + خادم الطباعة + الواجهة** على حاسبة Windows 11، ويُنشئ أيقونة **LiteRestaurant** على سطح المكتب.

> **قاعدة البيانات يدوية:** شغّل MySQL من XAMPP (أو أي MySQL) بنفسك. المثبّت لا يضمّن ولا يشغّل MariaDB.

> **ملاحظة:** لا تشغّل Litecashier (كاشير) و LiteRestaurant معاً على نفس الحاسبة — نفس المنافذ `5189` و `5000`.

## للمستخدم النهائي

1. أنشئ قاعدة بيانات باسم `pos` في phpMyAdmin / MySQL (مستخدم `root` بدون كلمة مرور).
2. شغّل **MySQL** من لوحة XAMPP.
3. انسخ `LiteRestaurant-Setup.exe` وثبّته.
4. افتح أيقونة **LiteRestaurant** على سطح المكتب.
5. سيفتح المتصفح على: `http://localhost:5189`

### إعدادات قاعدة البيانات (ثابتة في البرنامج)

| الحقل | القيمة |
|--------|--------|
| Server | `localhost` |
| Port | `3306` |
| Database | `pos` |
| User | `root` |
| Password | *(فارغ)* |

### تسجيل الدخول الافتراضي (أدمن النظام)

| الحقل | القيمة |
|--------|--------|
| الهاتف | `07830200030` |
| كلمة المرور | `12345678` |

> لا تُضاف بيانات تجريبية. الأدمن يُنشأ مع الـ migrations عند أول تشغيل ناجح، ثم تنشئ الحساب التجاري من صفحة الحسابات.

### في حال وجود مشكلة

- راجع مجلد السجلات: `C:\ProgramData\LiteRestaurant\Logs`
- تأكد أن MySQL يعمل وأن قاعدة `pos` موجودة
- المنافذ: `3306` (MySQL)، `5189` (النظام)، `5000` (الطباعة)

**ملاحظة:** الإصدار 1.0.1 — قاعدة البيانات خارجية (XAMPP). عند التحديث ثبّت فوق النسخة الحالية بدون إلغاء تثبيت. المثبّت يوقف LiteRestaurant / RestaurantPOS / PrintServer تلقائياً قبل استبدال الملفات.

### الوصول من حاسبة ثانية (نفس الراوتر)

1. على **حاسبة السيرفر**: شغّل LiteRestaurant واتركه مفتوحاً.
2. اعرف IP السيرفر (`ipconfig` → IPv4)، مثال: `192.168.1.10`
3. على **الحاسبة الثانية** افتح المتصفح:
   ```
   http://192.168.1.10:5189
   ```
4. المثبّت يفتح جدار الحماية للمنافذ `5189` (النظام) و `5000` (الطباعة) تلقائياً.
5. يفضّل تثبيت IP ثابت للسيرفر من إعدادات الراوتر (DHCP reservation).

> الطباعة من الحاسبة الثانية تمر عبر Print Server على السيرفر (نفس الـ IP، منفذ 5000). الطابعة يجب أن تكون معرّفة على حاسبة السيرفر.

### تحديث بدون فقدان البيانات

1. شغّل المثبّت الجديد فوق التثبيت الحالي (لا تضغط «إلغاء التثبيت»).
2. المثبّت يوقف النظام تلقائياً (`LiteRestaurant.exe` / `RestaurantPOS.exe` / `PrintServer.exe`) — لا حاجة لملف الإيقاف يدوياً.
3. بعد انتهاء التثبيت افتح LiteRestaurant من الأيقونة.

> ملف **إيقاف LiteRestaurant** ما زال متوفراً إن احتجت إغلاق النظام يدوياً خارج التحديث.

### إلغاء التثبيت

من **إعدادات Windows → التطبيقات → LiteRestaurant → إلغاء التثبيت**

---

## لبناء المثبّت (جهاز التطوير)

### المتطلبات

| الأداة | الإصدار |
|--------|---------|
| .NET SDK | 8.0+ |
| Node.js | 18+ |
| Inno Setup | 6.x |

تثبيت Inno Setup عبر winget:

```powershell
winget install JRSoftware.InnoSetup --accept-package-agreements --accept-source-agreements
```

### خطوات البناء

```powershell
cd restaurant_installer
powershell -ExecutionPolicy Bypass -File .\build-installer.ps1
```

الملف الناتج:

```
restaurant_installer/output/LiteRestaurant-Setup.exe
```

### خيارات البناء

```powershell
# بدون إعادة بناء الواجهة
.\build-installer.ps1 -SkipFrontendBuild

# تجهيز staging فقط بدون Inno Setup
.\build-installer.ps1 -SkipInstallerCompile
```

### ماذا يفعل المثبّت؟

```
C:\Program Files\LiteRestaurant\
├── LiteRestaurant.exe      ← أيقونة سطح المكتب
├── RestaurantPOS\          ← API + واجهة Vue (wwwroot)
└── PrintServer\            ← خادم الطباعة (منفذ 5000)
```

السجلات: `C:\ProgramData\LiteRestaurant\Logs`

### تدفق التشغيل

1. شغّل MySQL يدوياً (XAMPP) وتأكد من وجود قاعدة `pos`
2. **LiteRestaurant.exe** يشغّل PrintServer ثم RestaurantPOS
3. يفتح المتصفح على الواجهة

---

## ملاحظات

- **الحجم التقريبي:** أصغر بكثير بدون MariaDB المضمّن
- **بدون توقيع رقمي:** قد يظهر تحذير SmartScreen من Windows — اختر "تشغيل على أي حال"
- **التشغيل التلقائي مع ويندوز:** غير مفعّل — التشغيل يدوياً من الأيقونة فقط
