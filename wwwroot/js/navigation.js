(() => {
  "use strict";
  const page = document.getElementById("app-page");
  if (!page || !window.fetch) return;
  let busy = false;
  let pendingPop = null;
  let currentUrl = location.href;
  history.replaceState({ app: true }, "", currentUrl);

  const internal = url => url.origin === location.origin &&
    !/^\/Autenticacao(?:\/|$)/i.test(url.pathname);

  async function navigate(url, { method = "GET", body, pop = false } = {}) {
    if (busy) return;
    busy = true;
    page.setAttribute("aria-busy", "true");
    document.body.classList.add("app-loading");
    const controls = [...document.querySelectorAll('button[type="submit"], input[type="submit"]')];
    const enabled = controls.filter(control => !control.disabled);
    enabled.forEach(control => control.disabled = true);
    document.getElementById("navigation-error")?.remove();
    try {
      const response = await fetch(url, { method, body, credentials: "same-origin",
        headers: { "X-Requested-With": "fetch" }, cache: "no-store" });
      const finalUrl = new URL(response.url);
      if (!internal(finalUrl)) { location.assign(finalUrl.href); return; }
      if (!response.ok) throw new Error("HTTP " + response.status);
      if (!response.headers.get("content-type")?.includes("text/html")) {
        throw new Error("Resposta inesperada");
      }
      const next = new DOMParser().parseFromString(await response.text(), "text/html");
      const nextPage = next.getElementById("app-page");
      if (!nextPage) { location.assign(finalUrl.href); return; }
      // Novas telas com scripts proprios devem declarar seu ciclo de inicializacao.
      if (nextPage.querySelector("script")) throw new Error("Script de pagina nao suportado");
      const modal = document.querySelector(".modal.show");
      if (modal) {
        await new Promise(resolve => {
          modal.addEventListener("hidden.bs.modal", resolve, { once: true });
          bootstrap.Modal.getOrCreateInstance(modal).hide();
        });
      }
      document.querySelectorAll('[data-bs-toggle="dropdown"]').forEach(element =>
        window.bootstrap?.Dropdown.getInstance(element)?.hide());
      page.replaceChildren(...nextPage.childNodes);
      document.title = next.title;
      const headerTitle = document.querySelector(".app-header .text-muted + strong");
      const nextTitle = next.querySelector(".app-header .text-muted + strong");
      if (headerTitle && nextTitle) headerTitle.textContent = nextTitle.textContent;
      const activeLinks = new Set([...next.querySelectorAll(".sidebar-menu a.active")]
        .map(link => link.getAttribute("href")));
      document.querySelectorAll(".sidebar-menu a").forEach(link => {
        const active = activeLinks.has(link.getAttribute("href"));
        link.classList.toggle("active", active);
        if (active) link.setAttribute("aria-current", "page");
        else link.removeAttribute("aria-current");
      });
      if (pendingPop) return;
      const destination = finalUrl.href;
      if (pop || destination === currentUrl) history.replaceState({ app: true }, "", destination);
      else history.pushState({ app: true }, "", destination);
      currentUrl = destination;
      document.body.classList.remove("menu-aberto");
      document.dispatchEvent(new CustomEvent("app:navigated"));
      page.focus({ preventScroll: true });
      window.scrollTo(0, 0);
    } catch (error) {
      // Nunca repetir POST automaticamente: a operacao pode ter sido concluida.
      const alert = document.createElement("div");
      alert.id = "navigation-error";
      alert.className = "alert alert-danger";
      alert.setAttribute("role", "alert");
      alert.textContent = method === "POST"
        ? "Não foi possível confirmar o resultado. Consulte a listagem antes de enviar novamente."
        : "Não foi possível carregar a página. Tente novamente.";
      page.prepend(alert);
      if (pop && !pendingPop) history.replaceState({ app: true }, "", currentUrl);
    } finally {
      busy = false;
      enabled.forEach(control => control.disabled = false);
      page.removeAttribute("aria-busy");
      document.body.classList.remove("app-loading");
      if (pendingPop) {
        const destination = pendingPop;
        pendingPop = null;
        navigate(destination, { pop: true });
      }
    }
  }

  document.addEventListener("click", event => {
    if (event.defaultPrevented || event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
    const link = event.target.closest("a[href]");
    if (!link || link.hasAttribute("download") || link.dataset.noSpa !== undefined ||
        (link.target && link.target !== "_self") || link.hasAttribute("data-bs-toggle")) return;
    const url = new URL(link.href);
    if (!internal(url) || url.hash) return;
    event.preventDefault();
    navigate(url.href);
  });

  document.addEventListener("submit", event => {
    if (event.defaultPrevented) return;
    const form = event.target;
    const submitter = event.submitter;
    const url = new URL(submitter?.getAttribute("formaction") || form.action || location.href, location.href);
    const method = (submitter?.getAttribute("formmethod") || form.method || "GET").toUpperCase();
    if (!internal(url) || !["GET", "POST"].includes(method) || form.dataset.noSpa !== undefined ||
        (form.target && form.target !== "_self")) return;
    event.preventDefault();
    if (busy) return;
    if (form.dataset.confirmar && !window.confirm(form.dataset.confirmar)) return;
    const data = new FormData(form);
    if (submitter?.name) data.append(submitter.name, submitter.value);
    if (method === "GET") {
      url.search = new URLSearchParams(data).toString();
      navigate(url.href);
    } else {
      navigate(url.href, { method, body: data });
    }
  });

  window.addEventListener("popstate", () => {
    if (busy) { pendingPop = location.href; return; }
    navigate(location.href, { pop: true });
  });
})();