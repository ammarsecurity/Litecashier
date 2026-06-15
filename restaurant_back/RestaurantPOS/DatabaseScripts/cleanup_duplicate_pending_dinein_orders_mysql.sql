-- =============================================================================
-- pos2: إلغاء أوردرات DineIn Pending المكررة/اليتيمة (لا CurrentOrderId على الطاولة)
-- نفّذ backup أولاً: mysqldump -u root pos2 > pos2_backup.sql
-- =============================================================================

START TRANSACTION;

-- معاينة قبل التنفيذ
SELECT COUNT(*) AS pending_dinein_before
FROM customerorders
WHERE IsDeleted = 0 AND OrderType = 'DineIn' AND PaymentStatus = 'Pending';

SELECT COUNT(*) AS will_keep_active
FROM tables
WHERE IsDeleted = 0 AND CurrentOrderId IS NOT NULL;

SELECT COUNT(*) AS will_cancel_orphans
FROM customerorders o
WHERE o.IsDeleted = 0
  AND o.OrderType = 'DineIn'
  AND o.PaymentStatus = 'Pending'
  AND o.Id NOT IN (
    SELECT CurrentOrderId FROM tables WHERE IsDeleted = 0 AND CurrentOrderId IS NOT NULL
  );

-- إلغاء اليتيمة: Pending DineIn غير مربوطة كـ CurrentOrderId لأي طاولة
UPDATE customerorders o
SET o.IsDeleted = 1,
    o.OrderStatus = 'Cancelled',
    o.UpdateDate = NOW(6)
WHERE o.IsDeleted = 0
  AND o.OrderType = 'DineIn'
  AND o.PaymentStatus = 'Pending'
  AND o.Id NOT IN (
    SELECT CurrentOrderId FROM (
      SELECT CurrentOrderId FROM tables WHERE IsDeleted = 0 AND CurrentOrderId IS NOT NULL
    ) AS active_ids
  );

-- بعد التنفيذ
SELECT COUNT(*) AS pending_dinein_after
FROM customerorders
WHERE IsDeleted = 0 AND OrderType = 'DineIn' AND PaymentStatus = 'Pending';

COMMIT;
