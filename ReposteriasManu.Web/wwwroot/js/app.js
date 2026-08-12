const API = 'https://localhost:7255/api';

// ===== STATE =====
let currentSection = 'dashboard';
let editingId = null;
let customers = [], orders = [], products = [], decorations = [];

// ===== NAVIGATION =====
function navigate(section) {
  document.querySelectorAll('.nav-item').forEach(el => el.classList.remove('active'));
  document.querySelectorAll('.section').forEach(el => el.classList.remove('active'));
  document.querySelector(`[data-section="${section}"]`).classList.add('active');
  document.getElementById(section).classList.add('active');
  currentSection = section;

  const titles = {
    dashboard: ['Dashboard', 'Resumen general de la repostería'],
    customers: ['Clientes', 'Gestión de clientes registrados'],
    products: ['Productos', 'Catálogo de productos disponibles'],
    orders: ['Pedidos', 'Gestión de pedidos por encargo'],
    decorations: ['Decoraciones', 'Detalles de decoración por pedido']
  };
  document.getElementById('page-title').textContent = titles[section][0];
  document.getElementById('page-subtitle').textContent = titles[section][1];

  if (section === 'dashboard') loadDashboard();
  else if (section === 'customers') loadCustomers();
  else if (section === 'products') loadProducts();
  else if (section === 'orders') loadOrders();
  else if (section === 'decorations') loadDecorations();
}

// ===== TOAST =====
function showToast(msg, type = 'success') {
  const t = document.getElementById('toast');
  t.textContent = (type === 'success' ? '✅ ' : '❌ ') + msg;
  t.className = `toast ${type} show`;
  setTimeout(() => t.classList.remove('show'), 3000);
}

// ===== API HELPERS =====
async function apiFetch(endpoint, options = {}) {
  try {
    const res = await fetch(`${API}${endpoint}`, {
      headers: { 'Content-Type': 'application/json' },
      ...options
    });
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    if (res.status === 204) return null;
    return await res.json();
  } catch (e) {
    console.error(e);
    throw e;
  }
}

// ===== DASHBOARD =====
async function loadDashboard() {
  try {
    [customers, products, orders, decorations] = await Promise.all([
      apiFetch('/customer'),
      apiFetch('/product'),
      apiFetch('/order'),
      apiFetch('/decoration')
    ]);

    document.getElementById('stat-customers').textContent = customers.length;
    document.getElementById('stat-products').textContent = products.length;
    document.getElementById('stat-orders').textContent = orders.length;

    const pending = orders.filter(o => o.status === 'Pendiente').length;
    document.getElementById('stat-pending').textContent = pending;

    renderRecentOrders();
    renderStatusChart();
  } catch { showToast('Error cargando el dashboard', 'error'); }
}

function renderRecentOrders() {
  const recent = [...orders].slice(-5).reverse();
  const tbody = document.getElementById('recent-orders-body');
  if (!recent.length) {
    tbody.innerHTML = '<tr><td colspan="5" style="text-align:center;padding:24px;color:#9d6b7a">No hay pedidos aún</td></tr>';
    return;
  }
  tbody.innerHTML = recent.map(o => {
    const client = customers.find(c => c.id === o.customerId);
    return `
      <tr>
        <td>#${o.id}</td>
        <td>${client ? client.name + ' ' + client.lastName : 'N/A'}</td>
        <td>${formatDate(o.deliveryDate)}</td>
        <td>${statusBadge(o.status)}</td>
        <td class="actions-cell">
          <button class="btn-edit" onclick="navigate('orders')">Ver</button>
        </td>
      </tr>`;
  }).join('');
}

function renderStatusChart() {
  const counts = { Pendiente: 0, 'En Proceso': 0, Listo: 0, Entregado: 0 };
  orders.forEach(o => { if (counts[o.status] !== undefined) counts[o.status]++; });
  const total = orders.length || 1;

  document.getElementById('bar-pending').style.width = (counts['Pendiente'] / total * 100) + '%';
  document.getElementById('bar-process').style.width = (counts['En Proceso'] / total * 100) + '%';
  document.getElementById('bar-ready').style.width = (counts['Listo'] / total * 100) + '%';
  document.getElementById('bar-delivered').style.width = (counts['Entregado'] / total * 100) + '%';

  document.getElementById('count-pending').textContent = counts['Pendiente'];
  document.getElementById('count-process').textContent = counts['En Proceso'];
  document.getElementById('count-ready').textContent = counts['Listo'];
  document.getElementById('count-delivered').textContent = counts['Entregado'];
}

// ===== CUSTOMERS =====
async function loadCustomers() {
  const tbody = document.getElementById('customers-body');
  tbody.innerHTML = '<tr><td colspan="6" class="loading">Cargando...</td></tr>';
  try {
    customers = await apiFetch('/customer');
    renderCustomers(customers);
  } catch { showToast('Error cargando clientes', 'error'); }
}

function renderCustomers(data) {
  const tbody = document.getElementById('customers-body');
  if (!data.length) {
    tbody.innerHTML = '<tr><td colspan="6"><div class="empty-state"><div class="empty-icon">👥</div><p>No hay clientes registrados</p></div></td></tr>';
    return;
  }
  tbody.innerHTML = data.map(c => `
    <tr>
      <td>${c.id}</td>
      <td>${c.name} ${c.lastName}</td>
      <td>${c.email || '-'}</td>
      <td>${c.phone || '-'}</td>
      <td>${c.address || '-'}</td>
      <td class="actions-cell">
        <button class="btn-edit" onclick="openEditCustomer(${c.id})">✏️ Editar</button>
        <button class="btn-danger" onclick="deleteCustomer(${c.id})">🗑️ Borrar</button>
      </td>
    </tr>`).join('');
}

function openAddCustomer() {
  editingId = null;
  document.getElementById('customer-modal-title').textContent = 'Nuevo Cliente';
  document.getElementById('customer-form').reset();
  openModal('customer-modal');
}

async function openEditCustomer(id) {
  editingId = id;
  const c = customers.find(x => x.id === id);
  if (!c) return;
  document.getElementById('customer-modal-title').textContent = 'Editar Cliente';
  document.getElementById('c-name').value = c.name;
  document.getElementById('c-lastname').value = c.lastName;
  document.getElementById('c-email').value = c.email || '';
  document.getElementById('c-phone').value = c.phone || '';
  document.getElementById('c-address').value = c.address || '';
  openModal('customer-modal');
}

async function saveCustomer() {
  const body = {
    name: document.getElementById('c-name').value,
    lastName: document.getElementById('c-lastname').value,
    email: document.getElementById('c-email').value,
    phone: document.getElementById('c-phone').value,
    address: document.getElementById('c-address').value
  };
  try {
    if (editingId) {
      await apiFetch(`/customer/${editingId}`, { method: 'PUT', body: JSON.stringify({ id: editingId, ...body }) });
      showToast('Cliente actualizado');
    } else {
      await apiFetch('/customer', { method: 'POST', body: JSON.stringify(body) });
      showToast('Cliente creado');
    }
    closeModal('customer-modal');
    loadCustomers();
  } catch { showToast('Error guardando cliente', 'error'); }
}

async function deleteCustomer(id) {
  if (!confirm('¿Eliminar este cliente?')) return;
  try {
    await apiFetch(`/customer/${id}`, { method: 'DELETE' });
    showToast('Cliente eliminado');
    loadCustomers();
  } catch { showToast('Error eliminando cliente', 'error'); }
}

// ===== PRODUCTS =====
async function loadProducts() {
  const tbody = document.getElementById('products-body');
  tbody.innerHTML = '<tr><td colspan="6" class="loading">Cargando...</td></tr>';
  try {
    products = await apiFetch('/product');
    renderProducts(products);
  } catch { showToast('Error cargando productos', 'error'); }
}

function renderProducts(data) {
  const tbody = document.getElementById('products-body');
  if (!data.length) {
    tbody.innerHTML = '<tr><td colspan="6"><div class="empty-state"><div class="empty-icon">🎂</div><p>No hay productos registrados</p></div></td></tr>';
    return;
  }
  tbody.innerHTML = data.map(p => `
    <tr>
      <td>${p.id}</td>
      <td>${p.name}</td>
      <td>${p.flavor || '-'}</td>
      <td>${p.size || '-'}</td>
      <td>RD$ ${parseFloat(p.price).toFixed(2)}</td>
      <td class="actions-cell">
        <button class="btn-edit" onclick="openEditProduct(${p.id})">✏️ Editar</button>
        <button class="btn-danger" onclick="deleteProduct(${p.id})">🗑️ Borrar</button>
      </td>
    </tr>`).join('');
}

function openAddProduct() {
  editingId = null;
  document.getElementById('product-modal-title').textContent = 'Nuevo Producto';
  document.getElementById('product-form').reset();
  openModal('product-modal');
}

async function openEditProduct(id) {
  editingId = id;
  const p = products.find(x => x.id === id);
  if (!p) return;
  document.getElementById('product-modal-title').textContent = 'Editar Producto';
  document.getElementById('p-name').value = p.name;
  document.getElementById('p-description').value = p.description || '';
  document.getElementById('p-price').value = p.price;
  document.getElementById('p-flavor').value = p.flavor || '';
  document.getElementById('p-size').value = p.size || '';
  openModal('product-modal');
}

async function saveProduct() {
  const body = {
    name: document.getElementById('p-name').value,
    description: document.getElementById('p-description').value,
    price: parseFloat(document.getElementById('p-price').value),
    flavor: document.getElementById('p-flavor').value,
    size: document.getElementById('p-size').value
  };
  try {
    if (editingId) {
      await apiFetch(`/product/${editingId}`, { method: 'PUT', body: JSON.stringify({ id: editingId, ...body }) });
      showToast('Producto actualizado');
    } else {
      await apiFetch('/product', { method: 'POST', body: JSON.stringify(body) });
      showToast('Producto creado');
    }
    closeModal('product-modal');
    loadProducts();
  } catch { showToast('Error guardando producto', 'error'); }
}

async function deleteProduct(id) {
  if (!confirm('¿Eliminar este producto?')) return;
  try {
    await apiFetch(`/product/${id}`, { method: 'DELETE' });
    showToast('Producto eliminado');
    loadProducts();
  } catch { showToast('Error eliminando producto', 'error'); }
}

// ===== ORDERS =====
async function loadOrders() {
  const tbody = document.getElementById('orders-body');
  tbody.innerHTML = '<tr><td colspan="7" class="loading">Cargando...</td></tr>';
  try {
    [orders, customers] = await Promise.all([apiFetch('/order'), apiFetch('/customer')]);
    renderOrders(orders);
    populateCustomerSelect('o-customer-id');
  } catch { showToast('Error cargando pedidos', 'error'); }
}

function renderOrders(data) {
  const tbody = document.getElementById('orders-body');
  if (!data.length) {
    tbody.innerHTML = '<tr><td colspan="7"><div class="empty-state"><div class="empty-icon">📋</div><p>No hay pedidos registrados</p></div></td></tr>';
    return;
  }
  tbody.innerHTML = data.map(o => {
    const client = customers.find(c => c.id === o.customerId);
    return `
      <tr>
        <td>#${o.id}</td>
        <td>${client ? client.name + ' ' + client.lastName : 'Cliente #' + o.customerId}</td>
        <td>${formatDate(o.orderDate)}</td>
        <td>${formatDate(o.deliveryDate)}</td>
        <td>${statusBadge(o.status)}</td>
        <td>${o.notes || '-'}</td>
        <td class="actions-cell">
          <button class="btn-edit" onclick="openEditOrder(${o.id})">✏️ Editar</button>
          <button class="btn-danger" onclick="deleteOrder(${o.id})">🗑️ Borrar</button>
        </td>
      </tr>`;
  }).join('');
}

function populateCustomerSelect(selectId) {
  const sel = document.getElementById(selectId);
  const current = sel.value;
  sel.innerHTML = '<option value="">Seleccionar cliente...</option>' +
    customers.map(c => `<option value="${c.id}">${c.name} ${c.lastName}</option>`).join('');
  if (current) sel.value = current;
}

function openAddOrder() {
  editingId = null;
  document.getElementById('order-modal-title').textContent = 'Nuevo Pedido';
  document.getElementById('order-form').reset();
  populateCustomerSelect('o-customer-id');
  openModal('order-modal');
}

async function openEditOrder(id) {
  editingId = id;
  const o = orders.find(x => x.id === id);
  if (!o) return;
  document.getElementById('order-modal-title').textContent = 'Editar Pedido';
  document.getElementById('o-customer-id').value = o.customerId;
  document.getElementById('o-order-date').value = o.orderDate?.substring(0, 10);
  document.getElementById('o-delivery-date').value = o.deliveryDate?.substring(0, 10);
  document.getElementById('o-status').value = o.status;
  document.getElementById('o-notes').value = o.notes || '';
  openModal('order-modal');
}

async function saveOrder() {
  const body = {
    customerId: parseInt(document.getElementById('o-customer-id').value),
    orderDate: document.getElementById('o-order-date').value,
    deliveryDate: document.getElementById('o-delivery-date').value,
    status: document.getElementById('o-status').value,
    notes: document.getElementById('o-notes').value
  };
  try {
    if (editingId) {
      await apiFetch(`/order/${editingId}`, { method: 'PUT', body: JSON.stringify({ id: editingId, ...body }) });
      showToast('Pedido actualizado');
    } else {
      await apiFetch('/order', { method: 'POST', body: JSON.stringify(body) });
      showToast('Pedido creado');
    }
    closeModal('order-modal');
    loadOrders();
  } catch { showToast('Error guardando pedido', 'error'); }
}

async function deleteOrder(id) {
  if (!confirm('¿Eliminar este pedido?')) return;
  try {
    await apiFetch(`/order/${id}`, { method: 'DELETE' });
    showToast('Pedido eliminado');
    loadOrders();
  } catch { showToast('Error eliminando pedido', 'error'); }
}

// ===== DECORATIONS =====
async function loadDecorations() {
  const tbody = document.getElementById('decorations-body');
  tbody.innerHTML = '<tr><td colspan="6" class="loading">Cargando...</td></tr>';
  try {
    [decorations, orders, products] = await Promise.all([
      apiFetch('/decoration'),
      apiFetch('/order'),
      apiFetch('/product')
    ]);
    renderDecorations(decorations);
    populateOrderSelect('d-order-id');
    populateProductSelect('d-product-id');
  } catch { showToast('Error cargando decoraciones', 'error'); }
}

function renderDecorations(data) {
  const tbody = document.getElementById('decorations-body');
  if (!data.length) {
    tbody.innerHTML = '<tr><td colspan="6"><div class="empty-state"><div class="empty-icon">🌸</div><p>No hay decoraciones registradas</p></div></td></tr>';
    return;
  }
  tbody.innerHTML = data.map(d => `
    <tr>
      <td>${d.id}</td>
      <td>${d.type || '-'}</td>
      <td>${d.color || '-'}</td>
      <td>${d.message || '-'}</td>
      <td>Pedido #${d.orderId}</td>
      <td class="actions-cell">
        <button class="btn-edit" onclick="openEditDecoration(${d.id})">✏️ Editar</button>
        <button class="btn-danger" onclick="deleteDecoration(${d.id})">🗑️ Borrar</button>
      </td>
    </tr>`).join('');
}

function populateOrderSelect(selectId) {
  const sel = document.getElementById(selectId);
  sel.innerHTML = '<option value="">Seleccionar pedido...</option>' +
    orders.map(o => `<option value="${o.id}">Pedido #${o.id}</option>`).join('');
}

function populateProductSelect(selectId) {
  const sel = document.getElementById(selectId);
  sel.innerHTML = '<option value="">Seleccionar producto...</option>' +
    products.map(p => `<option value="${p.id}">${p.name}</option>`).join('');
}

function openAddDecoration() {
  editingId = null;
  document.getElementById('decoration-modal-title').textContent = 'Nueva Decoración';
  document.getElementById('decoration-form').reset();
  openModal('decoration-modal');
}

async function openEditDecoration(id) {
  editingId = id;
  const d = decorations.find(x => x.id === id);
  if (!d) return;
  document.getElementById('decoration-modal-title').textContent = 'Editar Decoración';
  document.getElementById('d-type').value = d.type || '';
  document.getElementById('d-color').value = d.color || '';
  document.getElementById('d-message').value = d.message || '';
  document.getElementById('d-order-id').value = d.orderId;
  document.getElementById('d-product-id').value = d.productId;
  openModal('decoration-modal');
}

async function saveDecoration() {
  const body = {
    type: document.getElementById('d-type').value,
    color: document.getElementById('d-color').value,
    message: document.getElementById('d-message').value,
    orderId: parseInt(document.getElementById('d-order-id').value),
    productId: parseInt(document.getElementById('d-product-id').value)
  };
  try {
    if (editingId) {
      await apiFetch(`/decoration/${editingId}`, { method: 'PUT', body: JSON.stringify({ id: editingId, ...body }) });
      showToast('Decoración actualizada');
    } else {
      await apiFetch('/decoration', { method: 'POST', body: JSON.stringify(body) });
      showToast('Decoración creada');
    }
    closeModal('decoration-modal');
    loadDecorations();
  } catch { showToast('Error guardando decoración', 'error'); }
}

async function deleteDecoration(id) {
  if (!confirm('¿Eliminar esta decoración?')) return;
  try {
    await apiFetch(`/decoration/${id}`, { method: 'DELETE' });
    showToast('Decoración eliminada');
    loadDecorations();
  } catch { showToast('Error eliminando decoración', 'error'); }
}

// ===== SEARCH =====
function searchTable(inputId, tbodyId) {
  const q = document.getElementById(inputId).value.toLowerCase();
  document.querySelectorAll(`#${tbodyId} tr`).forEach(row => {
    row.style.display = row.textContent.toLowerCase().includes(q) ? '' : 'none';
  });
}

// ===== MODAL HELPERS =====
function openModal(id) { document.getElementById(id).classList.add('open'); }
function closeModal(id) { document.getElementById(id).classList.remove('open'); }

// ===== UTILS =====
function formatDate(dateStr) {
  if (!dateStr) return '-';
  return new Date(dateStr).toLocaleDateString('es-DO', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

function statusBadge(status) {
  const map = {
    'Pendiente': 'badge-pending',
    'En Proceso': 'badge-process',
    'Listo': 'badge-ready',
    'Entregado': 'badge-delivered',
    'Cancelado': 'badge-cancelled'
  };
  return `<span class="badge ${map[status] || 'badge-pending'}">${status}</span>`;
}

// ===== INIT =====
document.addEventListener('DOMContentLoaded', () => {
  navigate('dashboard');
});
