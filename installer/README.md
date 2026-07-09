# Litecashier — مثبّت Windows 11 (جهاز واحد)

مثبّت يجهّز **قاعدة البيانات + الخادم + خادم الطباعة + الواجهة** على حاسبة Windows 11 فارغة، ويُنشئ أيقونة **Litecashier** على سطح المكتب.

## للمستخدم النهائي

1. انسخ `Litecashier-Setup.exe` إلى الحاسبة (USB أو شبكة).
2. شغّل المثبّت واتبع الخطوات (قد يطلب صلاحيات المسؤول).
3. بعد الانتهاء، اضغط أيقونة **Litecashier** على سطح المكتب.
4. انتظر 10–30 ثانية في أول تشغيل (تهيئة قاعدة البيانات).
5. سيفتح المتصفح تلقائياً على: `http://localhost:5189`

### تسجيل الدخول التجريبي الافتراضي

| الحقل | القيمة |
|--------|--------|
| الهاتف | `07800000001` |
| كلمة المرور | `12345678` |

### في حال وجود مشكلة

- راجع مجلد السجلات: `C:\ProgramData\Litecashier\Logs`
- إذا علّق على "جاري تشغيل النظام": أغلق Litecashier من مدير المهام ثم أعد فتحه
- تأكد أن المنافذ غير مستخدمة: `3306` (MariaDB)، `5189` (النظام)، `5000` (الطباعة)
- أعد تشغيل الأيقونة — إذا كانت الخدمات تعمل مسبقاً سيفتح المتصفح مباشرة

**ملاحظة:** الإصدار 1.0.4 يثبّت تلقائياً **Visual C++** و **WebView2** ويخزّن قاعدة البيانات في `C:\ProgramData\Litecashier`.

### إلغاء التثبيت

من **إعدادات Windows → التطبيقات → Litecashier → إلغاء التثبيت**

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
cd installer
powershell -ExecutionPolicy Bypass -File .\build-installer.ps1
```

الملف الناتج:

```
installer/output/Litecashier-Setup.exe
```

### خيارات البناء

```powershell
# بدون إعادة بناء الواجهة
.\build-installer.ps1 -SkipFrontendBuild

# بدون تنزيل MariaDB (إذا كان محفوظاً في deps/)
.\build-installer.ps1 -SkipMariaDbDownload

# تجهيز staging فقط بدون Inno Setup
.\build-installer.ps1 -SkipInstallerCompile
```

### ماذا يفعل المثبّت؟

```
C:\Program Files\Litecashier\
├── Litecashier.exe      ← أيقونة سطح المكتب
├── POS\                 ← API + واجهة Vue (wwwroot/app)
├── PrintServer\         ← خادم الطباعة (منفذ 5000)
├── mariadb\             ← قاعدة بيانات محمولة (منفذ 3306)
└── Logs\                ← سجلات التشغيل
```

### تدفق التشغيل

1. **Litecashier.exe** يشغّل MariaDB إن لم تكن تعمل
2. يشغّل PrintServer ثم POS (بيئة Production)
3. يفتح المتصفح على الواجهة

---

## ملاحظات

- **الحجم التقريبي:** 400–700 MB
- **بدون توقيع رقمي:** قد يظهر تحذير SmartScreen من Windows — اختر "تشغيل على أي حال"
- **بدون إنترنت على الجهاز المستهدف:** مدعوم (MariaDB و WebView2 مضمّنان في المثبّت)
- **التشغيل التلقائي مع ويندوز:** غير مفعّل — التشغيل يدوياً من الأيقونة فقط
