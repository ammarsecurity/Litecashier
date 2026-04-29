.PHONY: help build up down restart logs clean setup

help: ## عرض هذه الرسالة المساعدة
	@echo "أوامر Docker المتاحة:"
	@echo ""
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-15s\033[0m %s\n", $$1, $$2}'

setup: ## إعداد ملف .env من env.example
	@if [ ! -f .env ]; then \
		cp env.example .env; \
		echo "✓ تم إنشاء ملف .env من env.example"; \
		echo "⚠ يرجى تعديل ملف .env قبل التشغيل"; \
	else \
		echo "⚠ ملف .env موجود بالفعل"; \
	fi

build: ## بناء الصور
	docker-compose build

up: ## تشغيل الخدمات
	docker-compose up -d

down: ## إيقاف الخدمات
	docker-compose down

restart: ## إعادة تشغيل الخدمات
	docker-compose restart

logs: ## عرض السجلات
	docker-compose logs -f

logs-api: ## عرض سجلات API فقط
	docker-compose logs -f restaurant-api

logs-db: ## عرض سجلات MySQL فقط
	docker-compose logs -f mysql

ps: ## عرض حالة الحاويات
	docker-compose ps

clean: ## تنظيف الحاويات والصور
	docker-compose down -v
	docker system prune -f

rebuild: clean build up ## إعادة بناء كاملة

prod-up: ## تشغيل في وضع الإنتاج
	docker-compose -f docker-compose.prod.yml up -d

prod-down: ## إيقاف في وضع الإنتاج
	docker-compose -f docker-compose.prod.yml down

prod-logs: ## عرض سجلات الإنتاج
	docker-compose -f docker-compose.prod.yml logs -f

shell-api: ## الدخول إلى shell حاوية API
	docker exec -it restaurant_api bash

shell-db: ## الدخول إلى shell حاوية MySQL
	docker exec -it restaurant_mysql bash

db-backup: ## نسخ احتياطي لقاعدة البيانات
	@echo "جارٍ إنشاء نسخة احتياطية..."
	docker exec restaurant_mysql mysqldump -u $$(grep MYSQL_USER .env | cut -d '=' -f2) -p$$(grep MYSQL_PASSWORD .env | cut -d '=' -f2) $$(grep MYSQL_DATABASE .env | cut -d '=' -f2) > backup_$$(date +%Y%m%d_%H%M%S).sql
	@echo "✓ تم إنشاء النسخة الاحتياطية"

db-restore: ## استعادة قاعدة البيانات من ملف
	@read -p "أدخل اسم ملف النسخة الاحتياطية: " file; \
	docker exec -i restaurant_mysql mysql -u $$(grep MYSQL_USER .env | cut -d '=' -f2) -p$$(grep MYSQL_PASSWORD .env | cut -d '=' -f2) $$(grep MYSQL_DATABASE .env | cut -d '=' -f2) < $$file
	@echo "✓ تم استعادة قاعدة البيانات"

migrate: ## تشغيل migrations
	docker exec -it restaurant_api dotnet ef database update

health: ## فحص حالة الخدمات
	@echo "فحص حالة API..."
	@docker exec restaurant_api wget --no-verbose --tries=1 --spider http://localhost:5000/swagger/index.html && echo "✓ API يعمل" || echo "✗ API لا يعمل"
	@echo ""
	@echo "فحص حالة MySQL..."
	@docker exec restaurant_mysql mysqladmin ping -h localhost -u $$(grep MYSQL_USER .env | cut -d '=' -f2) -p$$(grep MYSQL_PASSWORD .env | cut -d '=' -f2) && echo "✓ MySQL يعمل" || echo "✗ MySQL لا يعمل"

