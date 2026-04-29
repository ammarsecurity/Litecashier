# حل مشاكل النشر (Troubleshooting)

## المشكلة: `Uncaught SyntaxError: Unexpected token '<'`

هذه المشكلة تحدث عندما يعيد الخادم ملف `index.html` بدلاً من ملفات JavaScript.

## خطوات التشخيص:

### 1. تحقق من ملفات JavaScript موجودة:
افتح المتصفح واذهب إلى:
```
https://your-domain.com/static/js/chunk-vendors.ac85c418.js
```

**إذا رأيت:**
- ✅ كود JavaScript → الملفات موجودة وتُخدم بشكل صحيح
- ❌ صفحة HTML → الخادم يعيد `index.html` بدلاً من الملف

### 2. استخدم صفحة الاختبار:
افتح `test.html` في المتصفح:
```
https://your-domain.com/test.html
```

ستقوم الصفحة بفحص تلقائي وتخبرك بالمشكلة.

### 3. تحقق من ملف .htaccess:

تأكد من:
- ملف `.htaccess` موجود في المجلد الرئيسي (`dist/`)
- محتوى الملف صحيح (يجب أن يحتوي على قواعد لمنع إعادة كتابة ملفات static)

### 4. تحقق من إعدادات الخادم:

#### Apache:
- تأكد من أن `mod_rewrite` مفعّل
- تأكد من أن `AllowOverride All` في إعدادات Apache

#### Nginx:
استخدم ملف `nginx.conf` الموجود في المشروع

### 5. حلول بديلة:

#### الحل 1: استخدام مسار نسبي
في `vue.config.js`:
```javascript
publicPath: './'
```

#### الحل 2: استخدام مسار مطلق كامل
في `vue.config.js`:
```javascript
publicPath: 'https://your-domain.com/'
```

#### الحل 3: التحقق من قاعدة البيانات
إذا كنت تستخدم قاعدة بيانات، تأكد من أن المسارات صحيحة

### 6. التحقق من Console:

افتح Developer Tools (F12) واذهب إلى Console:
- ابحث عن أخطاء 404
- ابحث عن أخطاء CORS
- ابحث عن أخطاء تحميل الملفات

### 7. التحقق من Network Tab:

في Developer Tools، اذهب إلى Network:
- ابحث عن طلبات ملفات `.js`
- تحقق من Status Code:
  - 200 = نجح
  - 404 = الملف غير موجود
  - 301/302 = إعادة توجيه (قد تكون المشكلة هنا)

## حلول سريعة:

### إذا كان الخادم Apache:
1. تأكد من وجود `.htaccess` في المجلد الرئيسي
2. تأكد من `mod_rewrite` مفعّل
3. جرب إضافة هذا في `.htaccess`:
```apache
<FilesMatch "\.(js|css)$">
    Header set Content-Type "application/javascript"
</FilesMatch>
```

### إذا كان الخادم Nginx:
استخدم ملف `nginx.conf` الموجود في المشروع

### إذا كان الخادم IIS (Windows):
أنشئ ملف `web.config`:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
  <system.webServer>
    <rewrite>
      <rules>
        <rule name="Handle History Mode" stopProcessing="true">
          <match url="(.*)" />
          <conditions logicalGrouping="MatchAll">
            <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
            <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
          </conditions>
          <action type="Rewrite" url="/" />
        </rule>
      </rules>
    </rewrite>
  </system.webServer>
</configuration>
```

## إذا استمرت المشكلة:

1. تحقق من logs الخادم
2. تأكد من أن جميع الملفات موجودة في `dist/`
3. جرب رفع الملفات يدوياً
4. تحقق من صلاحيات الملفات (chmod 644 للملفات، 755 للمجلدات)





