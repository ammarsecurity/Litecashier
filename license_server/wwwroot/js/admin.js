(() => {
  const STORAGE_KEY = "lc_license_admin_key";

  const $ = (id) => document.getElementById(id);

  const els = {
    loginView: $("loginView"),
    appView: $("appView"),
    loginForm: $("loginForm"),
    adminKeyInput: $("adminKeyInput"),
    loginError: $("loginError"),
    createForm: $("createForm"),
    durationType: $("durationType"),
    durationValue: $("durationValue"),
    durationValueWrap: $("durationValueWrap"),
    createError: $("createError"),
    createSuccess: $("createSuccess"),
    lastCreated: $("lastCreated"),
    lastCreatedCode: $("lastCreatedCode"),
    copyLastBtn: $("copyLastBtn"),
    keysBody: $("keysBody"),
    keysEmpty: $("keysEmpty"),
    keysError: $("keysError"),
    searchInput: $("searchInput"),
    filterProduct: $("filterProduct"),
    filterStatus: $("filterStatus"),
    refreshBtn: $("refreshBtn"),
    logoutBtn: $("logoutBtn"),
    detailModal: $("detailModal"),
    detailTitle: $("detailTitle"),
    detailBody: $("detailBody"),
    closeModalBtn: $("closeModalBtn"),
    statTotal: $("statTotal"),
    statActive: $("statActive"),
    statRevoked: $("statRevoked"),
    statActivations: $("statActivations"),
    announcementForm: $("announcementForm"),
    annError: $("annError"),
    annSuccess: $("annSuccess"),
    annEmpty: $("annEmpty"),
    annErrorList: $("annErrorList"),
    announcementsList: $("announcementsList"),
    devicesBody: $("devicesBody"),
    devicesEmpty: $("devicesEmpty"),
    devicesError: $("devicesError"),
    deviceSearch: $("deviceSearch"),
    deviceFilterProduct: $("deviceFilterProduct"),
    deviceFilterStatus: $("deviceFilterStatus"),
  };

  let keysCache = [];
  let announcementsCache = [];
  let devicesCache = [];
  let activeTab = "licenses";

  function getAdminKey() {
    return sessionStorage.getItem(STORAGE_KEY) || "";
  }
  function setAdminKey(key) {
    sessionStorage.setItem(STORAGE_KEY, key);
  }
  function clearAdminKey() {
    sessionStorage.removeItem(STORAGE_KEY);
  }

  function showLogin(message) {
    els.appView.hidden = true;
    els.loginView.hidden = false;
    if (message) {
      els.loginError.hidden = false;
      els.loginError.textContent = message;
    } else {
      els.loginError.hidden = true;
    }
  }

  function showApp() {
    els.loginView.hidden = true;
    els.appView.hidden = false;
  }

  async function api(path, options = {}) {
    const headers = {
      "Content-Type": "application/json",
      "X-Admin-Key": getAdminKey(),
      ...(options.headers || {}),
    };
    const res = await fetch(path, { ...options, headers });
    if (res.status === 401) {
      clearAdminKey();
      showLogin("مفتاح الأدمن غير صحيح");
      throw new Error("unauthorized");
    }
    const text = await res.text();
    let data = null;
    try {
      data = text ? JSON.parse(text) : null;
    } catch {
      data = { message: text };
    }
    if (!res.ok) {
      throw new Error(data?.message || `خطأ ${res.status}`);
    }
    return data;
  }

  function escapeHtml(value) {
    return String(value ?? "")
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
  }
  function escapeAttr(value) {
    return escapeHtml(value).replace(/'/g, "&#39;");
  }

  function formatDuration(type, value) {
    if (type === "Lifetime") return "مدى الحياة";
    if (type === "Months") return `${value} شهر`;
    return `${value} يوم`;
  }

  function formatDate(iso) {
    if (!iso) return "—";
    try {
      return new Date(iso).toLocaleString("ar-IQ", {
        year: "numeric",
        month: "short",
        day: "numeric",
        hour: "2-digit",
        minute: "2-digit",
      });
    } catch {
      return iso;
    }
  }

  function productLabel(p) {
    if (p === "Cashier") return "كاشير";
    if (p === "Restaurant") return "مطعم";
    if (p === "Both") return "كاشير + مطعم";
    return p;
  }

  function updateDurationVisibility() {
    const lifetime = els.durationType.value === "Lifetime";
    els.durationValueWrap.hidden = lifetime;
    if (lifetime) els.durationValue.value = "0";
    else if (!Number(els.durationValue.value)) els.durationValue.value = "2";
  }

  function setTab(name) {
    activeTab = name;
    document.querySelectorAll(".tab").forEach((t) => {
      t.classList.toggle("active", t.dataset.tab === name);
    });
    document.querySelectorAll(".tab-panel").forEach((p) => {
      p.hidden = p.id !== `tab-${name}`;
    });
  }

  async function refreshActive() {
    if (activeTab === "licenses") await loadKeys();
    else if (activeTab === "announcements") await loadAnnouncements();
    else await loadDevices();
  }

  // --- Licenses ---
  function updateStats(list) {
    const total = list.length;
    const revoked = list.filter((k) => k.isRevoked).length;
    const activations = list.reduce((sum, k) => sum + (k.activationCount || 0), 0);
    els.statTotal.textContent = String(total);
    els.statActive.textContent = String(total - revoked);
    els.statRevoked.textContent = String(revoked);
    els.statActivations.textContent = String(activations);
  }

  function filteredKeys() {
    const q = (els.searchInput.value || "").trim().toLowerCase();
    const product = els.filterProduct.value;
    const status = els.filterStatus.value;
    return keysCache.filter((k) => {
      if (product && k.product !== product) return false;
      if (status === "active" && k.isRevoked) return false;
      if (status === "revoked" && !k.isRevoked) return false;
      if (!q) return true;
      return `${k.code} ${k.notes || ""} ${k.product}`.toLowerCase().includes(q);
    });
  }

  function renderKeys() {
    const list = filteredKeys();
    updateStats(keysCache);
    els.keysBody.innerHTML = "";
    els.keysEmpty.hidden = list.length > 0;
    els.keysError.hidden = true;
    for (const key of list) {
      const tr = document.createElement("tr");
      tr.innerHTML = `
        <td class="code-cell">${escapeHtml(key.code)}</td>
        <td><span class="badge badge-soft">${escapeHtml(productLabel(key.product))}</span></td>
        <td>${escapeHtml(formatDuration(key.durationType, key.durationValue))}</td>
        <td>${key.activationCount || 0} / ${key.maxActivations}</td>
        <td>${key.isRevoked ? '<span class="badge badge-off">ملغى</span>' : '<span class="badge badge-ok">نشط</span>'}</td>
        <td>${escapeHtml(key.notes || "—")}</td>
        <td>${escapeHtml(formatDate(key.createdAt))}</td>
        <td>
          <div class="row-actions">
            <button type="button" class="btn btn-small" data-act="copy" data-code="${escapeAttr(key.code)}">نسخ</button>
            <button type="button" class="btn btn-small" data-act="details" data-code="${escapeAttr(key.code)}">التفعيلات</button>
            ${key.isRevoked ? "" : `<button type="button" class="btn btn-danger btn-small" data-act="revoke" data-code="${escapeAttr(key.code)}">إلغاء</button>`}
          </div>
        </td>`;
      els.keysBody.appendChild(tr);
    }
  }

  async function loadKeys() {
    els.keysError.hidden = true;
    try {
      keysCache = await api("/api/admin/keys");
      renderKeys();
    } catch (err) {
      if (err.message === "unauthorized") return;
      els.keysError.hidden = false;
      els.keysError.textContent = err.message || "تعذر تحميل السيريالات";
    }
  }

  async function copyText(text) {
    try {
      await navigator.clipboard.writeText(text);
    } catch {
      const ta = document.createElement("textarea");
      ta.value = text;
      document.body.appendChild(ta);
      ta.select();
      document.execCommand("copy");
      ta.remove();
    }
  }

  function openDetails(code) {
    const key = keysCache.find((k) => k.code === code);
    if (!key) return;
    els.detailTitle.textContent = `تفعيلات — ${key.code}`;
    const activations = key.activations || [];
    if (!activations.length) {
      els.detailBody.innerHTML = `<p class="empty-state">لا توجد تفعيلات على أجهزة بعد.</p>`;
    } else {
      els.detailBody.innerHTML = activations
        .map(
          (a) => `
        <article class="activation-card">
          <div><strong>الجهاز:</strong> <code>${escapeHtml(a.machineId)}</code></div>
          <div class="activation-meta">
            <div>المنتج: ${escapeHtml(productLabel(a.product))}</div>
            <div>التفعيل: ${escapeHtml(formatDate(a.activatedAt))}</div>
            <div>ينتهي: ${a.expiresAt ? escapeHtml(formatDate(a.expiresAt)) : "مدى الحياة"}</div>
            <div>آخر ظهور: ${escapeHtml(formatDate(a.lastSeenAt))}</div>
          </div>
        </article>`
        )
        .join("");
    }
    els.detailModal.hidden = false;
  }

  // --- Announcements ---
  function renderAnnouncements() {
    els.announcementsList.innerHTML = "";
    els.annEmpty.hidden = announcementsCache.length > 0;
    els.annErrorList.hidden = true;

    for (const a of announcementsCache) {
      const card = document.createElement("article");
      card.className = "ann-card";
      const dismissals = a.dismissals || [];
      card.innerHTML = `
        <h3>${escapeHtml(a.title)}</h3>
        <p>${escapeHtml(a.body || "")}</p>
        <div class="ann-meta">
          <span class="badge badge-soft">${escapeHtml(productLabel(a.productScope))}</span>
          <span class="badge ${a.isActive ? "badge-ok" : "badge-off"}">${a.isActive ? "نشط" : "متوقف"}</span>
          <span>ترتيب: ${a.sortOrder}</span>
          ${a.imageUrl ? `<span>صورة</span>` : ""}
        </div>
        <div class="ann-actions">
          <button type="button" class="btn btn-small" data-ann="toggle" data-id="${a.id}">${a.isActive ? "إيقاف" : "تفعيل"}</button>
          <button type="button" class="btn btn-small" data-ann="dismiss" data-id="${a.id}">إخفاء عن جهاز</button>
          <button type="button" class="btn btn-danger btn-small" data-ann="delete" data-id="${a.id}">حذف</button>
        </div>
        ${
          dismissals.length
            ? `<div class="dismissals"><strong>مخفي عن:</strong><br/>${dismissals
                .map(
                  (d) =>
                    `${escapeHtml(d.machineId)} (${escapeHtml(productLabel(d.product))})
                    <button type="button" class="btn btn-small" data-ann="undismiss" data-id="${a.id}" data-machine="${escapeAttr(
                      d.machineId
                    )}" data-product="${escapeAttr(d.product)}">إظهار</button>`
                )
                .join("<br/>")}</div>`
            : ""
        }`;
      els.announcementsList.appendChild(card);
    }
  }

  async function loadAnnouncements() {
    try {
      announcementsCache = await api("/api/admin/announcements");
      renderAnnouncements();
    } catch (err) {
      if (err.message === "unauthorized") return;
      els.annErrorList.hidden = false;
      els.annErrorList.textContent = err.message || "تعذر تحميل الإعلانات";
    }
  }

  // --- Devices ---
  function filteredDevices() {
    const q = (els.deviceSearch.value || "").trim().toLowerCase();
    const product = els.deviceFilterProduct.value;
    const status = els.deviceFilterStatus.value;
    return devicesCache.filter((d) => {
      if (product && d.product !== product) return false;
      if (status === "paused" && !d.isPaused) return false;
      if (status === "active" && d.isPaused) return false;
      if (!q) return true;
      return `${d.machineId} ${d.licenseCode || ""} ${d.product}`.toLowerCase().includes(q);
    });
  }

  function renderDevices() {
    const list = filteredDevices();
    els.devicesBody.innerHTML = "";
    els.devicesEmpty.hidden = list.length > 0;
    els.devicesError.hidden = true;
    for (const d of list) {
      const tr = document.createElement("tr");
      tr.innerHTML = `
        <td class="code-cell">${escapeHtml(d.machineId)}</td>
        <td><span class="badge badge-soft">${escapeHtml(productLabel(d.product))}</span></td>
        <td class="code-cell">${escapeHtml(d.licenseCode || "—")}</td>
        <td>${escapeHtml(formatDate(d.lastSeenAt))}</td>
        <td>${d.isPaused ? '<span class="badge badge-warn">متوقف</span>' : '<span class="badge badge-ok">نشط</span>'}</td>
        <td>${escapeHtml(d.pauseReason || "—")}</td>
        <td>
          <div class="row-actions">
            ${
              d.isPaused
                ? `<button type="button" class="btn btn-small" data-dev="resume" data-machine="${escapeAttr(
                    d.machineId
                  )}" data-product="${escapeAttr(d.product)}">استئناف</button>`
                : `<button type="button" class="btn btn-danger btn-small" data-dev="pause" data-machine="${escapeAttr(
                    d.machineId
                  )}" data-product="${escapeAttr(d.product)}">إيقاف</button>`
            }
            <button type="button" class="btn btn-small" data-dev="copy" data-machine="${escapeAttr(d.machineId)}">نسخ ID</button>
          </div>
        </td>`;
      els.devicesBody.appendChild(tr);
    }
  }

  async function loadDevices() {
    try {
      devicesCache = await api("/api/admin/devices");
      renderDevices();
    } catch (err) {
      if (err.message === "unauthorized") return;
      els.devicesError.hidden = false;
      els.devicesError.textContent = err.message || "تعذر تحميل الأجهزة";
    }
  }

  // Events
  document.querySelectorAll(".tab").forEach((tab) => {
    tab.addEventListener("click", async () => {
      setTab(tab.dataset.tab);
      await refreshActive();
    });
  });

  els.loginForm.addEventListener("submit", async (e) => {
    e.preventDefault();
    const key = els.adminKeyInput.value.trim();
    if (!key) return;
    setAdminKey(key);
    try {
      await api("/api/admin/ping");
      showApp();
      setTab("licenses");
      await loadKeys();
    } catch (err) {
      clearAdminKey();
      showLogin(err.message === "unauthorized" ? "مفتاح الأدمن غير صحيح" : err.message);
    }
  });

  els.logoutBtn.addEventListener("click", () => {
    clearAdminKey();
    showLogin();
  });

  els.refreshBtn.addEventListener("click", () => refreshActive());
  els.durationType.addEventListener("change", updateDurationVisibility);

  els.createForm.addEventListener("submit", async (e) => {
    e.preventDefault();
    els.createError.hidden = true;
    els.createSuccess.hidden = true;
    const durationType = els.durationType.value;
    const body = {
      product: $("product").value,
      durationType,
      durationValue: durationType === "Lifetime" ? 0 : Number(els.durationValue.value || 1),
      maxActivations: Number($("maxActivations").value || 1),
      notes: $("notes").value.trim() || null,
    };
    try {
      const created = await api("/api/admin/keys", { method: "POST", body: JSON.stringify(body) });
      els.createSuccess.hidden = false;
      els.createSuccess.textContent = "تم إنشاء السيريال بنجاح";
      els.lastCreated.hidden = false;
      els.lastCreatedCode.textContent = created.code;
      $("notes").value = "";
      await loadKeys();
    } catch (err) {
      els.createError.hidden = false;
      els.createError.textContent = err.message || "فشل إنشاء السيريال";
    }
  });

  els.copyLastBtn.addEventListener("click", async () => {
    const code = els.lastCreatedCode.textContent;
    if (code) await copyText(code);
  });

  els.searchInput.addEventListener("input", renderKeys);
  els.filterProduct.addEventListener("change", renderKeys);
  els.filterStatus.addEventListener("change", renderKeys);

  els.keysBody.addEventListener("click", async (e) => {
    const btn = e.target.closest("button[data-act]");
    if (!btn) return;
    const act = btn.getAttribute("data-act");
    const code = btn.getAttribute("data-code");
    if (!code) return;
    if (act === "copy") {
      await copyText(code);
      btn.textContent = "تم";
      setTimeout(() => (btn.textContent = "نسخ"), 900);
    } else if (act === "details") {
      openDetails(code);
    } else if (act === "revoke") {
      if (!confirm(`إلغاء السيريال ${code}؟`)) return;
      try {
        await api("/api/admin/revoke", { method: "POST", body: JSON.stringify({ code }) });
        await loadKeys();
      } catch (err) {
        alert(err.message || "فشل الإلغاء");
      }
    }
  });

  els.announcementForm.addEventListener("submit", async (e) => {
    e.preventDefault();
    els.annError.hidden = true;
    els.annSuccess.hidden = true;
    const body = {
      title: $("annTitle").value.trim(),
      body: $("annBody").value.trim(),
      imageUrl: $("annImageUrl").value.trim() || null,
      linkUrl: $("annLinkUrl").value.trim() || null,
      productScope: $("annScope").value,
      isActive: $("annActive").checked,
      sortOrder: Number($("annSort").value || 0),
    };
    try {
      await api("/api/admin/announcements", { method: "POST", body: JSON.stringify(body) });
      els.annSuccess.hidden = false;
      els.annSuccess.textContent = "تم حفظ الإعلان";
      els.announcementForm.reset();
      $("annActive").checked = true;
      $("annSort").value = "0";
      await loadAnnouncements();
    } catch (err) {
      els.annError.hidden = false;
      els.annError.textContent = err.message || "فشل الحفظ";
    }
  });

  els.announcementsList.addEventListener("click", async (e) => {
    const btn = e.target.closest("button[data-ann]");
    if (!btn) return;
    const act = btn.dataset.ann;
    const id = Number(btn.dataset.id);
    try {
      if (act === "delete") {
        if (!confirm("حذف هذا الإعلان؟")) return;
        await api(`/api/admin/announcements/${id}`, { method: "DELETE" });
      } else if (act === "toggle") {
        const item = announcementsCache.find((a) => a.id === id);
        if (!item) return;
        await api(`/api/admin/announcements/${id}`, {
          method: "PUT",
          body: JSON.stringify({ isActive: !item.isActive }),
        });
      } else if (act === "dismiss") {
        const machineId = prompt("MachineId للجهاز:");
        if (!machineId) return;
        const product = prompt("المنتج (Cashier أو Restaurant):", "Cashier");
        if (!product) return;
        await api(`/api/admin/announcements/${id}/dismiss`, {
          method: "POST",
          body: JSON.stringify({ machineId: machineId.trim(), product: product.trim() }),
        });
      } else if (act === "undismiss") {
        const qs = new URLSearchParams({
          machineId: btn.dataset.machine,
          product: btn.dataset.product,
        });
        await api(`/api/admin/announcements/${id}/dismiss?${qs.toString()}`, {
          method: "DELETE",
        });
      }
      await loadAnnouncements();
    } catch (err) {
      alert(err.message || "فشلت العملية");
    }
  });

  els.deviceSearch.addEventListener("input", renderDevices);
  els.deviceFilterProduct.addEventListener("change", renderDevices);
  els.deviceFilterStatus.addEventListener("change", renderDevices);

  els.devicesBody.addEventListener("click", async (e) => {
    const btn = e.target.closest("button[data-dev]");
    if (!btn) return;
    const act = btn.dataset.dev;
    const machineId = btn.dataset.machine;
    const product = btn.dataset.product;
    try {
      if (act === "copy") {
        await copyText(machineId);
        btn.textContent = "تم";
        setTimeout(() => (btn.textContent = "نسخ ID"), 900);
      } else if (act === "pause") {
        const reason = prompt("سبب الإيقاف (اختياري):") || "";
        await api("/api/admin/devices/pause", {
          method: "POST",
          body: JSON.stringify({ machineId, product, reason }),
        });
        await loadDevices();
      } else if (act === "resume") {
        await api("/api/admin/devices/resume", {
          method: "POST",
          body: JSON.stringify({ machineId, product }),
        });
        await loadDevices();
      }
    } catch (err) {
      alert(err.message || "فشلت العملية");
    }
  });

  els.closeModalBtn.addEventListener("click", () => {
    els.detailModal.hidden = true;
  });
  els.detailModal.addEventListener("click", (e) => {
    if (e.target === els.detailModal) els.detailModal.hidden = true;
  });

  updateDurationVisibility();
  (async () => {
    if (!getAdminKey()) {
      showLogin();
      return;
    }
    try {
      await api("/api/admin/ping");
      showApp();
      setTab("licenses");
      await loadKeys();
    } catch {
      clearAdminKey();
      showLogin();
    }
  })();
})();
