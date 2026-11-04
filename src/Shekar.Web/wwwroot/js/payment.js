/* شکار — پرداخت کارت‌به‌کارت و صفحه‌ی بررسی واریزی (بدون وابستگی) */
(function () {
  'use strict';

  var PLANS = {
    pro:   { name: 'شکارچی حرفه‌ای', price: '۱,۴۹۹,۰۰۰', raw: '1499000' },
    basic: { name: 'شکارچی',        price: '۹۹۰,۰۰۰',   raw: '990000' }
  };
  var MAX_FILE = 5 * 1024 * 1024;
  var MIN_TEXT = 20;
  var $ = function (sel, root) { return (root || document).querySelector(sel); };
  var $$ = function (sel, root) { return Array.prototype.slice.call((root || document).querySelectorAll(sel)); };
  var fa = function (x) { return String(x).replace(/\d/g, function (d) { return '۰۱۲۳۴۵۶۷۸۹'[d]; }); };

  function store(k, v) { try { sessionStorage.setItem('shekar.' + k, v); } catch (e) {} }
  function load(k) { try { return sessionStorage.getItem('shekar.' + k); } catch (e) { return null; } }

  function setPlan(key) {
    var p = PLANS[key] || PLANS.pro;
    $$('[data-price]').forEach(function (el) { el.textContent = p.price; });
    $$('[data-plan-name]').forEach(function (el) { el.textContent = p.name; });
    $$('[data-pro-only]').forEach(function (el) { el.hidden = key !== 'pro'; });
    return p;
  }

  /* ---------- toast + copy ---------- */
  var toastTimer;
  function toast(msg) {
    var t = $('[data-toast]'); if (!t) return;
    $('[data-toast-text]', t).textContent = msg; t.hidden = false;
    clearTimeout(toastTimer); toastTimer = setTimeout(function () { t.hidden = true; }, 2200);
  }
  function copy(text, btn, msg) {
    var done = function () {
      var lbl = $('[data-copy-label]', btn), old = lbl ? lbl.textContent : '';
      if (lbl) lbl.textContent = 'کپی شد';
      setTimeout(function () { if (lbl) lbl.textContent = old; }, 2000);
      toast(msg);
    };
    if (navigator.clipboard && navigator.clipboard.writeText) navigator.clipboard.writeText(text).then(done, done);
    else done();
  }

  /* ---------- pay.html ---------- */
  function initPay(form) {
    var planInput = $('[data-plan-input]', form), methodInput = $('[data-method-input]', form);
    var fileInput = $('[data-file]', form), textInput = $('[data-text]', form), submit = $('[data-submit]', form);
    var initial = planInput && (planInput.value === 'basic' || planInput.value === 'pro') ? planInput.value : 'pro';
    var current = initial, method = 'image', previewUrl = '';

    function validate() {
      var ok = method === 'image' ? !!(fileInput.files && fileInput.files[0]) : textInput.value.trim().length >= MIN_TEXT;
      submit.disabled = !ok;
      submit.className = ok ? 'btn btn-p' : 'btn btn-off';
      return ok;
    }

    $$('[data-plan]').forEach(function (b) {
      b.addEventListener('click', function () {
        current = b.getAttribute('data-plan'); planInput.value = current; setPlan(current);
        $$('[data-plan]').forEach(function (x) { var on = x === b; x.className = on ? 'segb segb-on' : 'segb'; x.setAttribute('aria-pressed', on); });
      });
    });

    $$('[data-copy]').forEach(function (b) { b.addEventListener('click', function () { copy(b.getAttribute('data-copy'), b, b.getAttribute('data-copy-msg')); }); });
    $$('[data-copy-amount]').forEach(function (b) { b.addEventListener('click', function () { copy(PLANS[current].raw, b, b.getAttribute('data-copy-msg')); }); });

    $$('[data-tab]').forEach(function (t) {
      t.addEventListener('click', function () {
        method = t.getAttribute('data-tab'); methodInput.value = method;
        $$('[data-tab]').forEach(function (x) { var on = x === t; x.className = on ? 'tab tab-on' : 'tab'; x.setAttribute('aria-selected', on); });
        $$('[data-panel]').forEach(function (p) { p.hidden = p.getAttribute('data-panel') !== method; });
        validate();
      });
    });

    var drop = $('[data-drop]', form), card = $('[data-file-card]', form), err = $('[data-file-error]', form);
    function showFile() {
      var f = fileInput.files && fileInput.files[0];
      err.hidden = true;
      if (f && (f.size > MAX_FILE || !/^image\//.test(f.type))) {
        err.textContent = f.size > MAX_FILE ? 'حجم عکس بیشتر از ۵ مگابایته.' : 'فقط عکس (JPG، PNG، WEBP) قبوله.';
        err.hidden = false; fileInput.value = ''; f = null;
      }
      if (previewUrl) { URL.revokeObjectURL(previewUrl); previewUrl = ''; }
      if (f) { previewUrl = URL.createObjectURL(f); $('[data-file-preview]', card).src = previewUrl; $('[data-file-name]', card).textContent = f.name; }
      drop.hidden = !!f; card.hidden = !f; validate();
    }
    fileInput.addEventListener('change', showFile);
    $('[data-file-clear]', form).addEventListener('click', function () { fileInput.value = ''; showFile(); });

    var hint = $('[data-text-hint]', form);
    textInput.addEventListener('input', function () {
      var n = textInput.value.trim().length;
      hint.textContent = n === 0 ? 'مبلغ، تاریخ و شماره پیگیری باید تو متن باشه.' : (n >= MIN_TEXT ? '✓ متن دریافت شد' : 'متن کوتاهه؛ کامل بچسبونش.');
      hint.className = n === 0 ? 'muted' : (n >= MIN_TEXT ? 'zok' : 'zwarn');
      validate();
    });

    form.addEventListener('submit', function (e) {
      e.preventDefault();
      if (!validate()) return;
      submit.disabled = true; submit.textContent = 'در حال ارسال…';
      var reviewUrl = form.getAttribute('data-review-url');
      var go = function (ref) {
        store('plan', current); store('method', method); if (ref) store('ref', ref);
        location.href = reviewUrl + '?plan=' + encodeURIComponent(current) + (ref ? '&ref=' + encodeURIComponent(ref) : '');
      };
      if (method === 'image' && previewUrl) {
        // پیش‌نمایش برای صفحه‌ی بررسی (فقط سمت مرورگر)
        try {
          var c = document.createElement('canvas'), img = $('[data-file-preview]', form), w = 520, h = Math.round(w * img.naturalHeight / Math.max(1, img.naturalWidth));
          c.width = w; c.height = h; c.getContext('2d').drawImage(img, 0, 0, w, h); store('receipt', c.toDataURL('image/jpeg', .7));
        } catch (x) {}
      } else { store('receipt', ''); }
      if (form.getAttribute('data-demo') === 'true') { go(''); return; }
      // ارسال واقعی به بک‌اند: پاسخ { paymentId, reference }
      fetch(form.action, { method: 'POST', body: new FormData(form), credentials: 'same-origin' })
        .then(function (r) { if (!r.ok) throw r; return r.json(); })
        .then(function (d) { go(d.reference || d.paymentId || ''); })
        .catch(function () { submit.disabled = false; submit.textContent = 'ارسال رسید و فعال‌سازی'; toast('ارسال نشد؛ دوباره امتحان کن.'); });
    });

    setPlan(current); validate();
  }

  /* ---------- pay-review.html ---------- */
  function initReview(root) {
    var q = new URLSearchParams(location.search);
    var plan = q.get('plan') || load('plan') || 'pro', p = setPlan(plan);
    var ref = q.get('ref') || load('ref'); if (ref) $$('[data-ref]').forEach(function (el) { el.textContent = ref.indexOf('SHK') === 0 ? ref : 'SHK-' + fa(ref); });
    var receipt = load('receipt'), img = $('[data-receipt-img]'), mock = $('[data-receipt-mock]');
    if (receipt && img) { img.src = receipt; img.hidden = false; mock.hidden = true; }

    var D = +root.getAttribute('data-duration') || 15000, K = D / 15000;
    var STEPS = [0, 2.2, 5.2, 8.6, 11.8].map(function (s) { return s * K * 1000; });
    var HEAD = ['رسیدت رسید؛ داریم آماده‌ش می‌کنیم', 'داریم اطلاعات رسید رو می‌خونیم', 'داریم مبلغ و کارت مقصد رو تطبیق می‌دیم', 'داریم شماره پیگیری رو استعلام می‌کنیم', 'همه‌چی درسته؛ داریم اشتراکت رو فعال می‌کنیم'];
    var FIELDS = { amount: 2.6, card: 3.4, date: 4.2, trace: 4.9 };
    var LOG = [[0.2, 'dim', '> receipt.upload', 'received'], [1.0, 'ok', '> normalize', 'ok'], [2.3, '', '> ocr.extract --lang fa', 'running'], [3.0, 'ok', '  amount', p.raw + ' IRT'], [3.6, 'ok', '  dest_card', '6219********8372'], [4.4, 'ok', '  date', 'parsed'], [5.0, 'ok', '  trace_no', 'parsed'], [5.6, '', '> match.amount', p.raw + ' == ' + p.raw], [6.6, 'ok', '  ✓ amount matched', ''], [7.6, 'ok', '  ✓ destination SAMAN …8372', ''], [8.8, '', '> bank.verify', 'pending'], [10.6, 'ok', '  ✓ transfer confirmed', ''], [12.0, '', '> subscription.activate', plan === 'pro' ? 'PRO_HUNTER' : 'HUNTER'], [13.6, 'ok', '  ✓ active', ''], [14.6, 'ok', '> done', '']];
    var logBox = $('[data-log]'), shown = 0, t0 = Date.now(), iv;

    function tick() {
      var ms = Math.min(D, Date.now() - t0), sec = ms / 1000 / K, idx = 0;
      STEPS.forEach(function (s, i) { if (ms >= s) idx = i; });
      var pct = Math.round(ms / D * 100);
      $('[data-pct]').textContent = fa(pct) + '٪';
      $('[data-left]').textContent = fa(Math.max(1, Math.ceil((D - ms) / 1000)));
      $('[data-bar]').style.width = pct + '%';
      $('[data-prog]').setAttribute('aria-valuenow', pct);
      $('[data-stepno]').textContent = fa(idx + 1);
      $('[data-headline]').textContent = HEAD[idx];
      $$('[data-step]').forEach(function (li, i) { li.className = 'chk ' + (i < idx ? 'chk-done' : (i === idx ? 'chk-run' : 'chk-wait')); });
      Object.keys(FIELDS).forEach(function (k) {
        var ok = sec >= FIELDS[k], f = $('[data-field="' + k + '"]'), hl = $('[data-hl="' + k + '"]');
        f.className = ok ? 'fld fld-ok' : 'fld';
        $('[data-skel]', f).hidden = ok; $$('[data-val]', f).forEach(function (v) { v.hidden = !ok; });
        if (hl) hl.className = ok ? 'mrow mrow-on' : 'mrow';
      });
      var sc = $('[data-scan]'); if (sc) sc.hidden = sec >= 8.6;
      while (shown < LOG.length && sec >= LOG[shown][0]) {
        var l = LOG[shown++], div = document.createElement('div');
        div.className = 'term-line ' + l[1]; div.textContent = l[2] + ' ';
        var sp = document.createElement('span'); sp.className = 'dim'; sp.textContent = l[3]; div.appendChild(sp);
        logBox.appendChild(div); while (logBox.children.length > 7) logBox.removeChild(logBox.firstChild);
      }
      if (ms >= D) finish();
    }
    function finish() {
      clearInterval(iv);
      $('[data-review]').hidden = true; $('[data-done]').hidden = false;
      var lbl = $('[data-stage-label]'); if (lbl) lbl.textContent = 'تموم شد!';
      try { sessionStorage.removeItem('shekar.receipt'); } catch (e) {}
      window.scrollTo(0, 0);
    }
    tick(); iv = setInterval(tick, 150);
  }

  document.addEventListener('DOMContentLoaded', function () {
    var form = $('[data-pay-form]'); if (form) initPay(form);
    var rv = $('[data-review-page]'); if (rv) initReview(rv);
  });
})();
