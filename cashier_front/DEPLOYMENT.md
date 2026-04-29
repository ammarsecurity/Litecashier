# تعليمات النشر (Deployment Instructions)

## المشكلة الشائعة: `Uncaught SyntaxError: Unexpected token '<'`

هذه المشكلة تحدث عندما يعيد الخادم ملف `index.html` بدلاً من ملفات JavaScript.

## الحلول:

### 1. Apache Server (استخدام .htaccess)

تأكد من أن ملف `.htaccess` موجود في مجلد `dist` بعد البناء.

إذا لم يكن موجوداً، انسخه يدوياً:
```bash
copy public\.htaccess dist\.htaccess
```

### 2. Nginx Server

استخدم ملف `nginx.conf` الموجود في المشروع وقم بتحديث المسار:
```nginx
root /path/to/your/dist;
```

### 3. التحقق من المسارات

تأكد من أن:
- جميع ملفات `dist` موجودة على الخادم
- المسارات النسبية صحيحة (`static/js/...` و `static/css/...`)
- ملف `.htaccess` موجود في المجلد الرئيسي

### 4. إذا استمرت المشكلة

جرب تغيير `publicPath` في `vue.config.js`:

```javascript
publicPath: process.env.NODE_ENV === 'production' ? '/' : '/',
```

ثم أعد البناء:
```bash
npm run build
```

### 5. التحقق من الخادم

- تأكد من أن mod_rewrite مفعّل في Apache
- تأكد من أن الخادم يخدم ملفات JavaScript بشكل صحيح
- تحقق من console المتصفح لرؤية أخطاء 404

## خطوات النشر:

1. قم ببناء المشروع:
   ```bash
   npm run build
   ```

2. انسخ ملف `.htaccess` إلى `dist`:
   ```bash
   copy public\.htaccess dist\.htaccess
   ```

3. ارفع محتويات مجلد `dist` إلى الخادم

4. تأكد من أن جميع الملفات موجودة:
   - `index.html`
   - `static/js/` (جميع ملفات JavaScript)
   - `static/css/` (جميع ملفات CSS)
   - `.htaccess`





