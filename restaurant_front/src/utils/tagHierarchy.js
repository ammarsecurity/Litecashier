/** فاصل العرض بين القسم الرئيسي والفرعي (يجب أن يطابق القيمة المخزنة في Item.Tags) */
export const TAG_SUB_SEPARATOR = " › ";

/**
 * معرف الأب من التاج (يتعامل مع PascalCase من الخادم وأنواع الرقم/النص)
 */
export function getTagParentId(tag) {
    if (!tag || typeof tag !== "object") return null;
    const v = tag.parentTagId ?? tag.ParentTagId;
    if (v === undefined || v === null || v === "") return null;
    return v;
}

/** هل يطابق معرف القسم الأب معرف الجذر المختار */
export function tagParentMatches(tag, parentId) {
    const pid = getTagParentId(tag);
    if (pid == null) return false;
    return String(pid) === String(parentId);
}

/**
 * نص عرض القسم (رئيسي أو رئيسي › فرعي)
 * @param {object} tag عنصر من API (parentTagId اختياري)
 * @param {Array<{id:number,name:string,parentTagId?:number|null}>} allTags
 */
export function tagDisplayName(tag, allTags) {
    if (!tag || !tag.name) return "";
    const pid = getTagParentId(tag);
    if (pid == null) return tag.name;
  const parent = (allTags || []).find(
    (t) => String(t.id ?? t.Id) === String(pid)
  );
  if (!parent) return tag.name;
  return `${parent.name}${TAG_SUB_SEPARATOR}${tag.name}`;
}

/** القيمة التي تُحفظ في حقل Tags للصنف (نفس منطق الفلتر في نقطة البيع) */
export function tagItemStorageValue(tag, allTags) {
  return tagDisplayName(tag, allTags);
}

/**
 * أزرار/خيارات التصفية في POS: قسم بلا أب، أو كل فرعي تحت أب له أبناء
 */
export function posCategoryEntries(allTags) {
  if (!allTags || !allTags.length) return [];
  const roots = allTags.filter((t) => getTagParentId(t) == null);
  const entries = [];
  for (const root of roots) {
    const subs = allTags.filter((t) =>
      tagParentMatches(t, root.id ?? root.Id)
    );
    if (subs.length === 0) {
      entries.push({ label: root.name, value: root.name });
    } else {
      for (const sub of subs) {
        const value = `${root.name}${TAG_SUB_SEPARATOR}${sub.name}`;
        entries.push({ label: value, value });
      }
    }
  }
  return entries;
}

/** أقسام جذر (بدون أب) مرتبة بالاسم */
export function rootTags(allTags) {
  if (!allTags || !allTags.length) return [];
  return allTags
    .filter((t) => getTagParentId(t) == null)
    .slice()
    .sort((a, b) => String(a.name || "").localeCompare(String(b.name || ""), "ar"));
}

/** أبناء قسم معيّن */
export function childTagsOf(parentTag, allTags) {
  if (!parentTag || !allTags || !allTags.length) return [];
  const pid = parentTag.id ?? parentTag.Id;
  if (pid === undefined || pid === null) return [];
  return allTags
    .filter((t) => tagParentMatches(t, pid))
    .slice()
    .sort((a, b) => String(a.name || "").localeCompare(String(b.name || ""), "ar"));
}

/**
 * قائمة اختيار ربط الطابعات: الجذور + الفروع بتسمية «أب › فرعي»
 * @returns {Array<{id:number|string, label:string, isRoot:boolean, parentId:number|string|null}>}
 */
export function tagsForPrinterSelect(allTags) {
  if (!allTags || !allTags.length) return [];
  const roots = rootTags(allTags);
  const entries = [];
  for (const root of roots) {
    const rid = root.id ?? root.Id;
    entries.push({
      id: rid,
      label: String(root.name ?? root.Name ?? ""),
      isRoot: true,
      parentId: null,
    });
    for (const sub of childTagsOf(root, allTags)) {
      const sid = sub.id ?? sub.Id;
      entries.push({
        id: sid,
        label: tagDisplayName(sub, allTags),
        isRoot: false,
        parentId: rid,
      });
    }
  }
  return entries.filter((e) => e.id != null && e.label);
}

/** تسمية عرض لربط TagPrinter (رئيسي أو رئيسي › فرعي) */
export function tagPrinterDisplayLabel(tagPrinter, allTags) {
  const tag = tagPrinter?.tag ?? tagPrinter?.Tag;
  if (!tag) return "";
  return tagDisplayName(tag, allTags || []) || String(tag.name ?? tag.Name ?? "");
}

/**
 * من النص المحفوظ في Item.Tags يستنتج معرف القسم الرئيسي والفرعي (إن وُجد)
 */
export function resolveItemTagsToCategoryIds(tagsStr, allTags) {
  if (!tagsStr || !allTags || !allTags.length) {
    return { rootId: null, subId: null };
  }
  const trimmed = String(tagsStr).trim();
  for (const t of allTags) {
    if (tagItemStorageValue(t, allTags) === trimmed) {
      const pid = getTagParentId(t);
      const tid = t.id ?? t.Id;
      if (pid == null) {
        return { rootId: tid, subId: null };
      }
      return { rootId: pid, subId: tid };
    }
  }
  const rootMatch = allTags.find(
    (x) => getTagParentId(x) == null && String(x.name) === trimmed
  );
  if (rootMatch) {
    return { rootId: rootMatch.id ?? rootMatch.Id, subId: null };
  }
  return { rootId: null, subId: null };
}

function normalizePrinterId(id) {
  if (id == null || id === "") return null;
  return String(id);
}

function addPrinterIds(targetSet, printerIds) {
  if (!printerIds) return;
  const list = Array.isArray(printerIds) ? printerIds : [printerIds];
  for (const id of list) {
    const normalized = normalizePrinterId(id);
    if (normalized == null) continue;
    targetSet.add(normalized);
  }
}

function pushUniquePrinter(map, key, printerId) {
  if (!key) return;
  const normalized = normalizePrinterId(printerId);
  if (normalized == null) return;
  if (!map[key]) map[key] = [];
  if (!map[key].includes(normalized)) {
    map[key].push(normalized);
  }
}

function isRootTagRef(tag, tagId, allTags) {
  if (tag && typeof tag === "object") {
    return getTagParentId(tag) == null;
  }
  if (tagId == null || !allTags?.length) return false;
  const found = allTags.find(
    (t) => String(t.id ?? t.Id) === String(tagId)
  );
  return !!(found && getTagParentId(found) == null);
}

/**
 * فهرس TagPrinter: كل tagId → قائمة طابعات، مع خرائط الاسم للرجوع
 * byRootTagName فقط لأسماء الأقسام الرئيسية لتفادي تصادم الاسم القصير مع فرعي
 */
function buildTagPrinterIndex(tagPrinters, allTags) {
  const byTagId = {};
  const byTagName = {};
  const byRootTagName = {};
  for (const tp of tagPrinters || []) {
    const tag = tp.tag ?? tp.Tag;
    const printer = tp.printer ?? tp.Printer;
    if (!tag && !printer && !tp.tagId && !tp.TagId) continue;
    const tagId = tag?.id ?? tag?.Id ?? tp.tagId ?? tp.TagId;
    const printerId =
      printer?.id ?? printer?.Id ?? tp.printerId ?? tp.PrinterId;
    const normalizedPrinterId = normalizePrinterId(printerId);
    if (normalizedPrinterId == null) continue;

    if (tagId != null) {
      pushUniquePrinter(byTagId, String(tagId), normalizedPrinterId);
    }

    const name = String(tag?.name ?? tag?.Name ?? "").trim();
    if (name) {
      pushUniquePrinter(byTagName, name, normalizedPrinterId);
      if (isRootTagRef(tag, tagId, allTags)) {
        pushUniquePrinter(byRootTagName, name, normalizedPrinterId);
      }
    }
    if (tag && allTags?.length) {
      const full = tagDisplayName(tag, allTags);
      if (full) {
        pushUniquePrinter(byTagName, full, normalizedPrinterId);
      }
    }
  }
  return { byTagId, byTagName, byRootTagName };
}

/**
 * كل طابعات القسم المستهدفة لصنف واحد:
 * - طابعة/طابعات الفرعي إن وُجدت
 * - وطابعة/طابعات الرئيسي دائماً إن وُجدت
 * (بدون تكرار لنفس printerId)
 */
export function resolvePrinterIdsForItemTags(itemTagsStr, tagPrinters, allTags) {
  if (!tagPrinters || !tagPrinters.length) return [];
  const trimmed = String(itemTagsStr || "").trim();
  if (!trimmed) return [];

  const { byTagId, byTagName, byRootTagName } = buildTagPrinterIndex(
    tagPrinters,
    allTags
  );
  const ids = new Set();
  const { rootId, subId } = resolveItemTagsToCategoryIds(trimmed, allTags);
  const rootPart = trimmed.split(TAG_SUB_SEPARATOR)[0].trim();

  if (subId != null) {
    addPrinterIds(ids, byTagId[String(subId)]);
    if (!byTagId[String(subId)]?.length) {
      // مسار العرض الكامل فقط — لا نستخدم الاسم القصير للفرعي لتجنب التوجيه الخاطئ
      addPrinterIds(ids, byTagName[trimmed]);
    }
  }

  if (rootId != null) {
    addPrinterIds(ids, byTagId[String(rootId)]);
    if (!byTagId[String(rootId)]?.length && rootPart) {
      addPrinterIds(ids, byRootTagName[rootPart]);
    }
  } else {
    addPrinterIds(ids, byTagName[trimmed]);
    if (rootPart) {
      addPrinterIds(ids, byRootTagName[rootPart]);
    }
  }

  return Array.from(ids);
}

/**
 * توافق: طابعة واحدة (أول هدف) لصنف من نص Item.Tags
 */
export function resolvePrinterIdForItemTags(itemTagsStr, tagPrinters, allTags) {
  const ids = resolvePrinterIdsForItemTags(itemTagsStr, tagPrinters, allTags);
  return ids.length ? ids[0] : null;
}

/**
 * تجميع أصناف الطلب حسب طابعات القسم؛
 * الصنف يُدفع لكل طابعة رئيسية/فرعية مستهدفة (fan-out).
 * الأصناف بلا طابعة في مجموعة unmapped
 */
export function groupItemsForDepartmentPrinting(items, tagPrinters, allTags) {
  const grouped = {};
  for (const item of items || []) {
    const tagName = item.tags || "مواد اخرى";
    const printerIds = resolvePrinterIdsForItemTags(tagName, tagPrinters, allTags);
    if (printerIds.length) {
      for (const printerId of printerIds) {
        const key = `printer_${printerId}`;
        if (!grouped[key]) {
          grouped[key] = {
            items: [],
            printerId,
            tagName,
          };
        }
        grouped[key].items.push(item);
      }
    } else {
      if (!grouped.unmapped) {
        grouped.unmapped = {
          items: [],
          printerId: null,
          tagName: "unmapped",
        };
      }
      grouped.unmapped.items.push(item);
    }
  }
  return grouped;
}
