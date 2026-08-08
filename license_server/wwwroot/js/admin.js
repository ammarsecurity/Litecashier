(() => {
  const STORAGE_KEY = "lc_license_admin_key";

  const els = {
    loginView: document.getElementById("loginView"),
    appView: document.getElementById("appView"),
    loginForm: document.getElementById("loginForm"),
    adminKeyInput: document.getElementById("adminKeyInput"),
    loginError: document.getElementById("loginError"),
    createForm: document.getElementById("createForm"),
    durationType: document.getElementById("durationType"),
    durationValue: document.getElementById("durationValue"),
    durationValueWrap: document.getElementById("durationValueWrap"),
    createError: document.getElementById("createError"),
    createSuccess: document.getElementById("createSuccess"),
    lastCreated: document.getElementById("lastCreated"),
    lastCreatedCode: document.getElementById("lastCreatedCode"),
    copyLastBtn: document.getElementById("copyLastBtn"),
    keysBody: document.getElementById("keysBody"),
    keysEmpty: document.getElementById("keysEmpty"),
    keysError: document.getElementById("keysError"),
    searchInput: document.getElementById("searchInput"),
    filterProduct: document.getElementById("filterProduct"),
    filterStatus: document.getElementById("filterStatus"),
    refreshBtn: document.getElementById("refreshBtn"),
    logoutBtn: document.getElementById("logoutBtn"),
    detailModal: document.getElementById("detailModal"),
    detailTitle: document.getElementById("detailTitle"),
    detailBody: document.getElementById("detailBody"),
    closeModalBtn: document.getElementById("closeModalBtn"),
    statTotal: document.getElementById("statTotal"),
    statActive: document.getElementById("statActive"),
    statRevoked: document.getElementById("statRevoked"),
    statActivations: document.getElementById("statActivations"),
  };

  let keysCache = [];

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
      const msg = data?.message || `خطأ ${res.status}`;
      throw new Error(msg);
    }
    return data;
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

  function updateStats(list) {
    const total = list.length;
    const revoked = list.filter((k) => k.isRevoked).length;
    const active = total - revoked;
    const activations = list.reduce((sum, k) => sum + (k.activationCount || 0), 0);
    els.statTotal.textContent = String(total);
    els.statActive.textContent = String(active);
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
      const hay = `${k.code} ${k.notes || ""} ${k.product}`.toLowerCase();
      return hay.includes(q);
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
        <td>${
          key.isRevoked
            ? '<span class="badge badge-off">ملغى</span>'
            : '<span class="badge badge-ok">نشط</span>'
        }</td>
        <td>${escapeHtml(key.notes || "—")}</td>
        <td>${escapeHtml(formatDate(key.createdAt))}</td>
        <td>
          <div class="row-actions">
            <button type="button" class="btn btn-small" data-act="copy" data-code="${escapeAttr(key.code)}">نسخ</button>
            <button type="button" class="btn btn-small" data-act="details" data-code="${escapeAttr(key.code)}">التفعيلات</button>
            ${
              key.isRevoked
                ? ""
                : `<button type="button" class="btn btn-danger btn-small" data-act="revoke" data-code="${escapeAttr(key.code)}">إلغاء</button>`
            }
          </div>
        </td>
      `;
      els.keysBody.appendChild(tr);
    }
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
      return true;
    } catch {
      const ta = document.createElement("textarea");
      ta.value = text;
      document.body.appendChild(ta);
      ta.select();
      document.execCommand("copy");
      ta.remove();
      return true;
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

  // Events
  els.loginForm.addEventListener("submit", async (e) => {
    e.preventDefault();
    const key = els.adminKeyInput.value.trim();
    if (!key) return;
    setAdminKey(key);
    try {
      await api("/api/admin/ping");
      showApp();
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

  els.refreshBtn.addEventListener("click", () => loadKeys());

  els.durationType.addEventListener("change", updateDurationVisibility);

  els.createForm.addEventListener("submit", async (e) => {
    e.preventDefault();
    els.createError.hidden = true;
    els.createSuccess.hidden = true;

    const durationType = els.durationType.value;
    const body = {
      product: document.getElementById("product").value,
      durationType,
      durationValue: durationType === "Lifetime" ? 0 : Number(els.durationValue.value || 1),
      maxActivations: Number(document.getElementById("maxActivations").value || 1),
      notes: document.getElementById("notes").value.trim() || null,
    };

    try {
      const created = await api("/api/admin/keys", {
        method: "POST",
        body: JSON.stringify(body),
      });
      els.createSuccess.hidden = false;
      els.createSuccess.textContent = "تم إنشاء السيريال بنجاح";
      els.lastCreated.hidden = false;
      els.lastCreatedCode.textContent = created.code;
      document.getElementById("notes").value = "";
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
      if (!confirm(`إلغاء السيريال ${code}؟ لن يعمل على الأجهزة بعد التحقق التالي.`)) return;
      try {
        await api("/api/admin/revoke", {
          method: "POST",
          body: JSON.stringify({ code }),
        });
        await loadKeys();
      } catch (err) {
        alert(err.message || "فشل الإلغاء");
      }
    }
  });

  els.closeModalBtn.addEventListener("click", () => {
    els.detailModal.hidden = true;
  });

  els.detailModal.addEventListener("click", (e) => {
    if (e.target === els.detailModal) els.detailModal.hidden = true;
  });

  // Boot
  updateDurationVisibility();
  if (getAdminKey()) {
    showApp();
    loadKeys();
  } else {
    showLogin();
  }
})();
