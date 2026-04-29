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
