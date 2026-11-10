import { useEffect, useState } from 'react';
import './App.css';
import HomeDashboard from './components/HomeDashboard.jsx';
import WishlistScreen from './screens/WishlistScreen.jsx';
import GameTrackingScreen from './screens/GameTrackingScreen.jsx';
import LibraryScreen from './screens/LibraryScreen.jsx';
import LibraryProgressSummary from './components/LibraryProgressSummary.jsx';
import PainelRetrospectiva from './components/PainelRetrospectiva.jsx';
import LibraryGameForm from './components/LibraryGameForm.jsx';

const plataformas = ['PlayStation 5', 'PlayStation 4', 'PlayStation 3', 'PlayStation 2', 'Xbox Series X|S', 'Xbox One', 'Nintendo Switch', 'PC', 'STEAM', 'EPIC', 'Amazon Luna', 'GOG', 'AMAZON PRIME', 'BLIZZARD', 'EA', 'NINTENDO', 'PSN', 'UBISOFT', 'XBOX', 'PLAY STORE', 'Outra'];

const Icon = ({ children, size = 20 }) => (
  <svg aria-hidden="true" width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
    {children}
  </svg>
);

function App() {
  const [tela, setTela] = useState('inicio');
  const [itens, setItens] = useState([]);
  const [dashboard, setDashboard] = useState(null);
  const [erroDashboard, setErroDashboard] = useState(false);
  const [perfil, setPerfil] = useState({ nome: 'Jogador', fotoDataUrl: null, exibirJogosRecentes: true });
  const [carregandoPerfil, setCarregandoPerfil] = useState(false);
  const [colecoes, setColecoes] = useState([]);
  const [nomeColecao, setNomeColecao] = useState('');
  const [descricaoColecao, setDescricaoColecao] = useState('');
  const [carregando, setCarregando] = useState(true);
  const [salvando, setSalvando] = useState(false);
  const [mensagem, setMensagem] = useState(null);
  const [jogoEmEdicao, setJogoEmEdicao] = useState(null);
  const [editorAberto, setEditorAberto] = useState(false);
  const [resumoProgresso, setResumoProgresso] = useState({ total: 0, zerados: 0, dropados: 0, naoTerminados: 0 });
  const totalJogosPerfil = dashboard?.resumo?.totalJogos ?? itens.length;

  useEffect(() => {
    if (tela !== 'inicio') return;
    setErroDashboard(false);
    fetch('/api/dashboard/inicio')
      .then((resposta) => resposta.ok ? resposta.json() : Promise.reject())
      .then((dados) => { setDashboard(dados); if (dados.perfil) setPerfil(dados.perfil); })
      .catch(() => { setDashboard(null); setErroDashboard(true); });
  }, [tela]);

  useEffect(() => {
    if (tela !== 'biblioteca' && tela !== 'colecoes') return;
    const carregarBiblioteca = async () => {
      try {
        const resposta = await fetch('/biblioteca');
        if (!resposta.ok) throw new Error();
        setItens(await resposta.json());
      } catch {
        setMensagem({ tipo: 'erro', texto: 'Não foi possível carregar sua biblioteca. Verifique se a API está ligada.' });
      } finally {
        setCarregando(false);
      }
    };

    carregarBiblioteca();
    fetch('/api/acompanhamentos-jogos/resumo')
      .then((resposta) => resposta.ok ? resposta.json() : Promise.reject())
      .then(setResumoProgresso)
      .catch(() => setMensagem({ tipo: 'erro', texto: 'A Biblioteca foi carregada, mas o resumo de progresso não está disponível.' }));
  }, [tela]);

  useEffect(() => {
    if (tela !== 'colecoes') return;
    fetch('/api/colecoes').then((resposta) => resposta.ok ? resposta.json() : Promise.reject())
      .then(setColecoes).catch(() => setMensagem({ tipo: 'erro', texto: 'Não foi possível carregar suas coleções.' }));
  }, [tela]);

  useEffect(() => {
    if (tela !== 'configuracoes') return;
    setCarregandoPerfil(true);
    fetch('/api/perfil').then((resposta) => resposta.ok ? resposta.json() : Promise.reject())
      .then(setPerfil).catch(() => setMensagem({ tipo: 'erro', texto: 'Não foi possível carregar seu perfil.' }))
      .finally(() => setCarregandoPerfil(false));
  }, [tela]);

  const salvarJogo = async (payload, editando = false) => {
    setMensagem(null);
    setSalvando(true);
    try {
      const resposta = await fetch('/biblioteca', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });

      const conteudo = await resposta.json().catch(() => ({}));
      if (!resposta.ok) throw new Error(conteudo.detail || 'Não foi possível salvar o jogo.');

      setItens((atuais) => {
        const demais = atuais.filter((item) => item.jogoId !== conteudo.jogoId);
        return [...demais, conteudo].sort((a, b) => a.titulo.localeCompare(b.titulo, 'pt-BR'));
      });
      setMensagem({
        tipo: 'sucesso',
        texto: editando
          ? `${conteudo.titulo} foi atualizado na sua biblioteca.`
          : `${conteudo.titulo} foi adicionado à sua biblioteca.`
      });
      return conteudo;
    } catch (erro) {
      setMensagem({ tipo: 'erro', texto: erro.message });
      throw erro;
    } finally {
      setSalvando(false);
    }
  };

  const abrirEdicao = (jogo) => {
    setJogoEmEdicao(jogo);
    setEditorAberto(true);
    setMensagem(null);
  };

  const cancelarEdicao = () => {
    setJogoEmEdicao(null);
    setEditorAberto(false);
    setMensagem(null);
  };

  const enviarJogo = async (payload) => {
    const editando = Boolean(jogoEmEdicao);
    const salvo = await salvarJogo(payload, editando);
    if (editando) {
      setJogoEmEdicao(null);
      setEditorAberto(false);
    }
    return salvo;
  };

  const criarColecao = async (evento) => {
    evento.preventDefault();
    try {
      const resposta = await fetch('/api/colecoes', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ nome: nomeColecao, descricao: descricaoColecao || null }) });
      if (!resposta.ok) throw new Error();
      const criada = await resposta.json();
      setColecoes((atuais) => [...atuais, criada]); setNomeColecao(''); setDescricaoColecao(''); setMensagem(null);
    } catch { setMensagem({ tipo: 'erro', texto: 'Não foi possível criar a coleção.' }); }
  };

  const adicionarNaColecao = async (colecaoId, jogoId) => {
    if (!jogoId) return;
    try {
      const resposta = await fetch(`/api/colecoes/${colecaoId}/jogos`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ jogoId: Number(jogoId) }) });
      if (!resposta.ok) throw new Error();
      const jogo = itens.find((item) => item.jogoId === Number(jogoId));
      setColecoes((atuais) => atuais.map((item) => item.id === colecaoId ? { ...item, jogos: [...item.jogos, { id: Number(jogoId), titulo: jogo?.titulo }] } : item));
      setMensagem(null);
    } catch { setMensagem({ tipo: 'erro', texto: 'Não foi possível adicionar o jogo à coleção.' }); }
  };

  const salvarPerfil = async (evento) => {
    evento.preventDefault();
    try {
      const resposta = await fetch('/api/perfil', { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(perfil) });
      if (!resposta.ok) throw new Error();
      setMensagem({ tipo: 'sucesso', texto: 'Configurações salvas.' });
    } catch { setMensagem({ tipo: 'erro', texto: 'Não foi possível salvar as configurações.' }); }
  };

  const carregarFoto = (arquivo) => {
    if (!arquivo) return;
    if (arquivo.size > 1_450_000) return setMensagem({ tipo: 'erro', texto: 'Escolha uma imagem menor que 1,4 MB.' });
    const leitor = new FileReader();
    leitor.onload = () => setPerfil((atual) => ({ ...atual, fotoDataUrl: leitor.result }));
    leitor.readAsDataURL(arquivo);
  };

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <button className="brand brand-button" onClick={() => setTela('inicio')} aria-label="GameLife">
          <span className="brand-mark"><Icon size={25}><path d="M7 6h10l3 9a2.3 2.3 0 0 1-4 2l-2-2h-4l-2 2a2.3 2.3 0 0 1-4-2z"/><path d="M8 10v4M6 12h4M16 11h.01M18 13h.01"/></Icon></span>
          <span>Game<span>Life</span></span>
        </button>

        <nav aria-label="Menu principal">
          <button className={`nav-item ${tela === 'inicio' ? 'active' : ''}`} onClick={() => setTela('inicio')}>
            <Icon><path d="m3 11 9-8 9 8"/><path d="M5 10v10h14V10M9 20v-6h6v6"/></Icon>
            Início
          </button>
          <button className={`nav-item ${tela === 'biblioteca' ? 'active' : ''}`} onClick={() => setTela('biblioteca')}>
            <Icon><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"/><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"/></Icon>
            Biblioteca
          </button>
          <button className={`nav-item ${tela === 'lista-desejos' ? 'active' : ''}`} onClick={() => setTela('lista-desejos')}>
            <Icon><path d="M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.7l-1.1-1.1a5.5 5.5 0 0 0-7.8 7.8L12 21l8.8-8.6a5.5 5.5 0 0 0 0-7.8z"/></Icon>
            Lista de Desejos
          </button>
          <button className={`nav-item ${tela === 'jogos-concluidos' ? 'active' : ''}`} onClick={() => setTela('jogos-concluidos')}>
            <Icon><circle cx="12" cy="12" r="9"/><path d="m8 12 3 3 5-6"/></Icon>
            Jogos concluídos
          </button>
          <button className={`nav-item ${tela === 'retrospectiva' ? 'active' : ''}`} onClick={() => setTela('retrospectiva')}>
            <Icon><path d="M4 20V10M10 20V4M16 20v-7M22 20V7"/></Icon>
            Retrospectiva
          </button>
          <button className={`nav-item ${tela === 'colecoes' ? 'active' : ''}`} onClick={() => setTela('colecoes')}>
            <Icon><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/></Icon>
            Coleções
          </button>
          <button className={`nav-item ${tela === 'configuracoes' ? 'active' : ''}`} onClick={() => setTela('configuracoes')}>
            <Icon><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.7 1.7 0 0 0 .3 1.9l.1.1-2.8 2.8-.1-.1a1.7 1.7 0 0 0-1.9-.3 1.7 1.7 0 0 0-1 1.6v.2h-4V21a1.7 1.7 0 0 0-1-1.6 1.7 1.7 0 0 0-1.9.3l-.1.1L4.2 17l.1-.1a1.7 1.7 0 0 0 .3-1.9A1.7 1.7 0 0 0 3 14H2.8v-4H3a1.7 1.7 0 0 0 1.6-1 1.7 1.7 0 0 0-.3-1.9L4.2 7 7 4.2l.1.1a1.7 1.7 0 0 0 1.9.3A1.7 1.7 0 0 0 10 3V2.8h4V3a1.7 1.7 0 0 0 1 1.6 1.7 1.7 0 0 0 1.9-.3l.1-.1L19.8 7l-.1.1a1.7 1.7 0 0 0-.3 1.9 1.7 1.7 0 0 0 1.6 1h.2v4H21a1.7 1.7 0 0 0-1.6 1z"/></Icon>
            Configurações
          </button>
        </nav>

        <div className="sidebar-footer">
          {perfil.fotoDataUrl ? <img className="avatar profile-photo" src={perfil.fotoDataUrl} alt="Foto do perfil" /> : <span className="avatar">{perfil.nome?.slice(0, 2).toUpperCase()}</span>}
          <span><strong>{perfil.nome || 'Jogador'}</strong><small>{totalJogosPerfil} {totalJogosPerfil === 1 ? 'jogo' : 'jogos'}</small></span>
        </div>
      </aside>

      {tela === 'inicio' && <main id="inicio">
        <header className="page-header"><div><span className="eyebrow">VISÃO GERAL</span><h1>Início</h1><p>Seu espaço pessoal para acompanhar jogos e conquistas.</p></div></header>
        {erroDashboard && <div className="feedback erro page-feedback" role="alert">Não foi possível carregar o resumo do banco local.</div>}
        <HomeDashboard dashboard={dashboard} perfil={perfil} />
      </main>}

      {tela === 'biblioteca' && <main id="biblioteca">
        <header className="page-header">
          <div>
            <span className="eyebrow">SUA COLEÇÃO</span>
            <h1>Minha Biblioteca de Jogos</h1>
            <p>Organize os jogos que fazem parte da sua história.</p>
          </div>
          <div className="collection-count"><strong>{itens.length}</strong><span>{itens.length === 1 ? 'jogo na coleção' : 'jogos na coleção'}</span></div>
        </header>

        <LibraryScreen items={itens} loading={carregando} onEdit={abrirEdicao} />
        <LibraryProgressSummary summary={resumoProgresso} />

        <details
          className="library-add-panel form-card"
          open={editorAberto}
          onToggle={(evento) => setEditorAberto(evento.currentTarget.open)}
        >
          <summary>
            <div className="card-heading">
              <span className="heading-icon"><Icon><path d="M12 5v14M5 12h14"/></Icon></span>
              <div>
                <h2>{jogoEmEdicao ? 'Editar jogo' : 'Adicionar novo jogo'}</h2>
                <p>{jogoEmEdicao ? 'Marque todas as plataformas onde você possui este jogo.' : 'Cadastre um jogo na sua coleção.'}</p>
              </div>
            </div>
          </summary>

          <LibraryGameForm
            key={jogoEmEdicao?.jogoId ?? 'novo'}
            platforms={plataformas}
            saving={salvando}
            onSubmit={enviarJogo}
            initialGame={jogoEmEdicao}
            onCancel={cancelarEdicao}
          />
          {mensagem && <div className={`feedback ${mensagem.tipo}`} role="alert">{mensagem.texto}</div>}
        </details>
      </main>}

      {tela === 'lista-desejos' && <WishlistScreen />}

      {tela === 'jogos-concluidos' && <GameTrackingScreen />}
      {tela === 'retrospectiva' && <PainelRetrospectiva />}

      {tela === 'colecoes' && <main id="colecoes"><header className="page-header"><div><span className="eyebrow">LISTAS PERSONALIZADAS</span><h1>Coleções</h1><p>Agrupe os jogos da biblioteca do seu jeito.</p></div></header>{mensagem && <div className={`feedback page-feedback ${mensagem.tipo}`} role="alert">{mensagem.texto}</div>}<section className="content-grid"><div className="form-card"><h2>Nova coleção</h2><form onSubmit={criarColecao}><label htmlFor="nome-colecao">Nome</label><input id="nome-colecao" value={nomeColecao} onChange={(e) => setNomeColecao(e.target.value)} required /><label htmlFor="descricao-colecao">Descrição</label><input id="descricao-colecao" value={descricaoColecao} onChange={(e) => setDescricaoColecao(e.target.value)} /><button type="submit">Criar coleção</button></form></div><section className="collection-list">{colecoes.length === 0 ? <div className="panel-card"><h2>Nenhuma coleção</h2><p>Crie uma lista para organizar jogos por tema, prioridade ou momento.</p></div> : colecoes.map((colecao) => <article className="panel-card" key={colecao.id}><h2>{colecao.nome}</h2><p>{colecao.descricao}</p><label htmlFor={`jogo-${colecao.id}`}>Adicionar jogo</label><select id={`jogo-${colecao.id}`} defaultValue="" onChange={(e) => { adicionarNaColecao(colecao.id, e.target.value); e.target.value = ''; }}><option value="">Selecione da biblioteca</option>{itens.filter((jogo) => !colecao.jogos.some((v) => v.id === jogo.jogoId)).map((jogo) => <option key={jogo.jogoId} value={jogo.jogoId}>{jogo.titulo}</option>)}</select><div className="tag-list">{colecao.jogos.map((jogo) => <span key={jogo.id}>{jogo.titulo}</span>)}</div></article>)}</section></section></main>}

      {tela === 'configuracoes' && <main id="configuracoes"><header className="page-header"><div><span className="eyebrow">PREFERÊNCIAS LOCAIS</span><h1>Configurações</h1><p>Personalize como o GameLife aparece neste computador.</p></div></header><section className="settings-card form-card">{carregandoPerfil ? <p>Carregando perfil...</p> : <form onSubmit={salvarPerfil}><label htmlFor="nome-perfil">Nome de usuário</label><input id="nome-perfil" value={perfil.nome || ''} onChange={(e) => setPerfil((atual) => ({ ...atual, nome: e.target.value }))} required /><label htmlFor="foto-perfil">Foto do perfil</label><input id="foto-perfil" className="file-input" type="file" accept="image/*" onChange={(e) => carregarFoto(e.target.files[0])} /><label className="setting-toggle"><input type="checkbox" checked={perfil.exibirJogosRecentes ?? true} onChange={(e) => setPerfil((atual) => ({ ...atual, exibirJogosRecentes: e.target.checked }))} /> Exibir jogos recentes no início</label>{mensagem && <div className={`feedback ${mensagem.tipo}`} role="alert">{mensagem.texto}</div>}<button type="submit">Salvar configurações</button></form>}</section></main>}
    </div>
  );
}

export default App;
