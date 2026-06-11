let confirmInstance = null;
let pendingResolve = null;

export function setConfirmInstance(vm) {
  confirmInstance = vm;
}

export function resolveConfirm(result) {
  if (!pendingResolve) return;
  const done = pendingResolve;
  pendingResolve = null;
  done(!!result);
}

function normalizeOptions(input, extra = {}) {
  if (typeof input === 'string') {
    return { message: input, ...extra };
  }
  return { ...input, ...extra };
}

export function showConfirm(input, extra = {}) {
  const options = normalizeOptions(input, extra);

  return new Promise((resolve) => {
    if (pendingResolve) {
      pendingResolve(false);
    }
    pendingResolve = resolve;

    if (confirmInstance && typeof confirmInstance.open === 'function') {
      confirmInstance.open(options);
      return;
    }

    pendingResolve = null;
    resolve(window.confirm(options.message || ''));
  });
}

export function confirm(input, extra) {
  return showConfirm(input, extra);
}
