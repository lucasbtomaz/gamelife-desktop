import { useEffect, useState } from 'react';
import { createGameTracking, deleteGameTracking, listGameTrackings, updateGameTracking } from '../api/gameTracking.js';

const platforms = ['PlayStation 5', 'PlayStation 4', 'PlayStation 3', 'PlayStation 2', 'Xbox Series X|S', 'Xbox One', 'Nintendo Switch', 'PC', 'STEAM', 'EPIC', 'Amazon Luna', 'GOG', 'AMAZON PRIME', 'BLIZZARD', 'EA', 'NINTENDO', 'PSN', 'UBISOFT', 'XBOX', 'PLAY STORE', 'Outra'];
const emptyForm = { titulo: '', plataforma: '', status: 'NaoTerminado', dataStatus: '', tempoEmHoras: '', conquistasPossiveis: '', conquistasObtidas: '', adicionarBiblioteca: false };
const statusLabels = { NaoTerminado: 'Não terminado', Zerado: 'Zerado', Dropado: 'Dropado' };

function dateLabel(status) {
  if (status === 'Zerado') return 'Data da conclusão';
  if (status === 'Dropado') return 'Data do abandono';
  return 'Data de atualização';
}

function toPayload(form) {
  const optionalNumber = (value) => value === '' ? null : Number(value);
  return {
    ...form,
    titulo: form.titulo.trim(),
    plataforma: form.plataforma.trim(),
    dataStatus: form.dataStatus || null,
    tempoEmHoras: optionalNumber(form.tempoEmHoras),
    conquistasPossiveis: optionalNumber(form.conquistasPossiveis),
    conquistasObtidas: optionalNumber(form.conquistasObtidas)
  };
}

export default function GameTrackingScreen() {
  const [items, setItems] = useState([]);
  const [form, setForm] = useState(emptyForm);
  const [editingId, setEditingId] = useState(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState(null);

  useEffect(() => {
    listGameTrackings().then(setItems)
      .catch((error) => setMessage({ type: 'erro', text: error.message }))
      .finally(() => setLoading(false));
  }, []);

  const change = (field) => (event) => setForm((current) => ({
    ...current,
    [field]: event.target.type === 'checkbox' ? event.target.checked : event.target.value
  }));

  const save = async (event) => {
    event.preventDefault();
    setMessage(null);
    if (form.conquistasObtidas !== '' && form.conquistasPossiveis === '') {
      setMessage({ type: 'erro', text: 'Informe as conquistas possíveis.' });
      return;
    }
    setSaving(true);
    try {
      const saved = editingId
        ? await updateGameTracking(editingId, toPayload(form))
        : await createGameTracking(toPayload(form));
      setItems((current) => editingId
        ? current.map((item) => item.id === editingId ? saved : item)
        : [...current, saved].sort((a, b) => a.titulo.localeCompare(b.titulo, 'pt-BR')));
      setForm(emptyForm);
      setEditingId(null);
      setMessage({ type: 'sucesso', text: 'Acompanhamento salvo.' });
    } catch (error) {
      setMessage({ type: 'erro', text: error.message });
    } finally {
      setSaving(false);
    }
  };

  const edit = (item) => {
    setEditingId(item.id);
    setForm({
      titulo: item.titulo,
      plataforma: item.plataforma,
      status: item.status,
      dataStatus: item.dataStatus?.slice(0, 10) || '',
      tempoEmHoras: item.tempoEmHoras ?? '',
      conquistasPossiveis: item.conquistasPossiveis ?? '',
      conquistasObtidas: item.conquistasObtidas ?? '',
      adicionarBiblioteca: item.estaNaBiblioteca
    });
    setMessage(null);
  };

  const remove = async (item) => {
    if (!window.confirm(`Excluir o acompanhamento de ${item.titulo}?`)) return;
    try {
      await deleteGameTracking(item.id);
      setItems((current) => current.filter((candidate) => candidate.id !== item.id));
      if (editingId === item.id) { setEditingId(null); setForm(emptyForm); }
    } catch (error) {
      setMessage({ type: 'erro', text: error.message });
    }
  };

  return <main id="jogos-concluidos">
    <header className="page-header"><div><span className="eyebrow">SEU PROGRESSO</span><h1>Jogos concluídos</h1><p>Acompanhe jogos zerados, dropados e ainda não terminados.</p></div></header>
    <section className="tracking-layout">
      <div className="form-card tracking-form">
        <h2>{editingId ? 'Editar acompanhamento' : 'Adicionar jogo'}</h2>
        <form onSubmit={save}>
          <label htmlFor="tracking-title">Nome do jogo</label>
          <input id="tracking-title" value={form.titulo} onChange={change('titulo')} maxLength="200" required />
          <label htmlFor="tracking-platform">Plataforma</label>
          <select id="tracking-platform" value={form.plataforma} onChange={change('plataforma')} required><option value="">Selecione</option>{platforms.map((item) => <option key={item}>{item}</option>)}</select>
          <label htmlFor="tracking-status">Status</label>
          <select id="tracking-status" value={form.status} onChange={change('status')}><option value="NaoTerminado">Não terminado</option><option value="Zerado">Zerado</option><option value="Dropado">Dropado</option></select>
          <label htmlFor="tracking-date">{dateLabel(form.status)}</label>
          <input id="tracking-date" type="date" value={form.dataStatus} onChange={change('dataStatus')} />
          <div className="tracking-numbers">
            <span><label htmlFor="tracking-hours">Tempo em horas</label><input id="tracking-hours" type="number" min="0" step="0.1" value={form.tempoEmHoras} onChange={change('tempoEmHoras')} /></span>
            <span><label htmlFor="tracking-possible">Conquistas possíveis</label><input id="tracking-possible" type="number" min="0" value={form.conquistasPossiveis} onChange={change('conquistasPossiveis')} /></span>
            <span><label htmlFor="tracking-earned">Conquistas obtidas</label><input id="tracking-earned" type="number" min="0" value={form.conquistasObtidas} onChange={change('conquistasObtidas')} /></span>
          </div>
          <label className="setting-toggle"><input type="checkbox" checked={form.adicionarBiblioteca} onChange={change('adicionarBiblioteca')} /> Adicionar à Biblioteca</label>
          {message && <div className={`feedback ${message.type}`} role="alert">{message.text}</div>}
          <div className="tracking-form-actions"><button type="submit" disabled={saving}>{editingId ? 'Atualizar acompanhamento' : 'Salvar acompanhamento'}</button>{editingId && <button type="button" className="secondary-button" onClick={() => { setEditingId(null); setForm(emptyForm); }}>Cancelar</button>}</div>
        </form>
      </div>
      <section className="tracking-list" aria-label="Jogos acompanhados">
        {loading ? <div className="panel-card">Carregando...</div> : items.length === 0 ? <div className="panel-card tracking-empty"><h2>Nenhum jogo acompanhado</h2><p>Cadastre um jogo para começar a registrar seu progresso.</p></div> : items.map((item) => <article className="tracking-card" key={item.id}>
          <div className="tracking-card-head"><div><span className={`status-badge status-${item.status}`}>{statusLabels[item.status]}</span><h2>{item.titulo}</h2><p>{item.plataforma}{item.estaNaBiblioteca ? ' · Na Biblioteca' : ''}</p></div><div><button type="button" className="history-toggle" aria-label={`Editar ${item.titulo}`} onClick={() => edit(item)}>Editar</button><button type="button" className="danger-link" aria-label={`Excluir ${item.titulo}`} onClick={() => remove(item)}>Excluir</button></div></div>
          <div className="tracking-details"><span><small>Tempo</small><strong>{item.tempoEmHoras ?? 0} h</strong></span><span><small>Conquistas</small><strong>{item.conquistasObtidas ?? 0}/{item.conquistasPossiveis ?? 0}</strong></span><span><small>Data</small><strong>{item.dataStatus ? new Date(item.dataStatus).toLocaleDateString('pt-BR', { timeZone: 'UTC' }) : 'Não informada'}</strong></span></div>
          <div className="achievement-progress"><div><span>Progresso das conquistas</span><strong>{item.percentualConquistas}%</strong></div><span className="progress-track"><span style={{ width: `${Math.max(0, Math.min(100, item.percentualConquistas))}%` }} /></span></div>
        </article>)}
      </section>
    </section>
  </main>;
}
