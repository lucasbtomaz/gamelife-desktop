import { useEffect, useState } from 'react';
import './App.css';
import HomeDashboard from './components/HomeDashboard.jsx';
import WishlistScreen from './screens/WishlistScreen.jsx';

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
  const [titulo, setTitulo] = useState('');
  const [plataforma, setPlataforma] = useState('');
  const [midiaFisica, setMidiaFisica] = useState(false);
  const [carregando, setCarregando] = useState(true);
  const [salvando, setSalvando] = useState(false);
  const [mensagem, setMensagem] = useState(null);
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

  const adicionarJogo = async (evento) => {
    evento.preventDefault();
    setMensagem(null);

    if (!titulo.trim() || !plataforma) {
      setMensagem({ tipo: 'erro', texto: 'Preencha o nome do jogo e escolha uma plataforma.' });
      return;
    }

    setSalvando(true);
    try {
      const resposta = await fetch('/biblioteca', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ titulo: titulo.trim(), plataforma, midiaFisica })
      });

      const conteudo = await resposta.json().catch(() => ({}));
      if (!resposta.ok) throw new Error(conteudo.detail || 'Não foi possível adicionar o jogo.');

      setItens((atuais) => [...atuais, { ...conteudo, midiaFisica }].sort((a, b) => a.titulo.localeCompare(b.titulo, 'pt-BR')));
      setTitulo('');
      setPlataforma('');
      setMidiaFisica(false);
      setMensagem({ tipo: 'sucesso', texto: `${conteudo.titulo} foi adicionado à sua biblioteca.` });
    } catch (erro) {
      setMensagem({ tipo: 'erro', texto: erro.message });
    } finally {
      setSalvando(false);
    }
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
          <button className={`nav-item ${tela === 'inicio' ? 'active' : ''}`} onClick={() => setTela('inicio')}>⌂ Início</button>
          <button className={`nav-item ${tela === 'biblioteca' ? 'active' : ''}`} onClick={() => setTela('biblioteca')}>
            <Icon><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"/><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"/></Icon>
            Biblioteca
          </button>
          <button className={`nav-item ${tela === 'lista-desejos' ? 'active' : ''}`} onClick={() => setTela('lista-desejos')}>♡ Lista de Desejos</button>
          <button className={`nav-item ${tela === 'colecoes' ? 'active' : ''}`} onClick={() => setTela('colecoes')}>▦ Coleções</button>
          <button className={`nav-item ${tela === 'configuracoes' ? 'active' : ''}`} onClick={() => setTela('configuracoes')}>⚙ Configurações</button>
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
            <h1>Biblioteca</h1>
            <p>Organize os jogos que fazem parte da sua história.</p>
          </div>
          <div className="collection-count"><strong>{itens.length}</strong><span>{itens.length === 1 ? 'jogo na coleção' : 'jogos na coleção'}</span></div>
        </header>

        <section className="content-grid">
          <div className="form-card">
            <div className="card-heading">
              <span className="heading-icon"><Icon><path d="M12 5v14M5 12h14"/></Icon></span>
              <div><h2>Adicionar novo jogo</h2><p>Cadastre um jogo na sua coleção.</p></div>
            </div>

            <form onSubmit={adicionarJogo}>
              <label htmlFor="titulo">Nome do jogo</label>
              <div className="input-wrap">
                <Icon><circle cx="11" cy="11" r="7"/><path d="m20 20-4-4"/></Icon>
                <input id="titulo" value={titulo} onChange={(e) => setTitulo(e.target.value)} maxLength="200" placeholder="Ex.: The Legend of Zelda" autoComplete="off" />
              </div>

              <label htmlFor="plataforma">Plataforma</label>
              <div className="select-wrap">
                <Icon><rect x="3" y="6" width="18" height="12" rx="3"/><path d="M8 10v4M6 12h4M16 11h.01M18 13h.01"/></Icon>
                <select id="plataforma" value={plataforma} onChange={(e) => setPlataforma(e.target.value)}>
                  <option value="" disabled>Selecione uma plataforma</option>
                  {plataformas.map((item) => <option key={item}>{item}</option>)}
                </select>
              </div>

              <label className={`media-type ${midiaFisica ? 'selected' : ''}`} htmlFor="midia-fisica">
                <span className="disc-icon"><Icon size={22}><circle cx="12" cy="12" r="9"/><circle cx="12" cy="12" r="2"/><path d="M12 3a9 9 0 0 1 9 9"/></Icon></span>
                <span><strong>Mídia física</strong><small>Marque se você possui uma cópia física deste jogo.</small></span>
                <input id="midia-fisica" aria-label="Mídia física" className="media-checkbox" type="checkbox" checked={midiaFisica} onChange={(e) => setMidiaFisica(e.target.checked)} />
                <span className="check" aria-hidden="true">{midiaFisica && <Icon size={15}><path d="m4 8 3 3 5-6"/></Icon>}</span>
              </label>

              {mensagem && <div className={`feedback ${mensagem.tipo}`} role="alert">{mensagem.texto}</div>}

              <button type="submit" disabled={salvando}>
                {salvando ? 'Adicionando...' : 'Adicionar à biblioteca'}
                {!salvando && <Icon><path d="M5 12h14M13 6l6 6-6 6"/></Icon>}
              </button>
            </form>
          </div>

          <section className="library-panel" aria-labelledby="meus-jogos">
            <div className="library-heading"><div><span className="eyebrow">COLEÇÃO ATUAL</span><h2 id="meus-jogos">Meus jogos</h2></div><span className="pill">BIBLIOTECA</span></div>

            {carregando ? (
              <div className="empty-state"><span className="loader"/><h3>Carregando coleção...</h3></div>
            ) : itens.length === 0 ? (
              <div className="empty-state">
                <span className="empty-illustration"><Icon size={50}><rect x="4" y="3" width="16" height="18" rx="2"/><path d="M9 8h6M9 12h6M9 16h3"/></Icon></span>
                <h3>Sua estante está esperando</h3>
                <p>Adicione seu primeiro jogo usando o formulário ao lado.</p>
              </div>
            ) : (
              <div className="game-list">
                {itens.map((item) => (
                  <article className="game-item" key={item.id || `${item.titulo}-${item.plataforma}`}>
                    <span className="game-cover"><Icon size={25}><path d="M6 3h12v18H6zM9 7h6M9 11h6"/></Icon></span>
                    <span className="game-info"><strong>{item.titulo}</strong><small>{item.plataforma}</small></span>
                    <span className={`physical-tag ${item.midiaFisica ? '' : 'not-physical'}`}><span/>{item.midiaFisica ? 'Física' : 'Digital'}</span>
                  </article>
                ))}
              </div>
            )}
          </section>
        </section>
      </main>}

      {tela === 'lista-desejos' && <WishlistScreen />}

      {tela === 'colecoes' && <main id="colecoes"><header className="page-header"><div><span className="eyebrow">LISTAS PERSONALIZADAS</span><h1>Coleções</h1><p>Agrupe os jogos da biblioteca do seu jeito.</p></div></header>{mensagem && <div className={`feedback page-feedback ${mensagem.tipo}`} role="alert">{mensagem.texto}</div>}<section className="content-grid"><div className="form-card"><h2>Nova coleção</h2><form onSubmit={criarColecao}><label htmlFor="nome-colecao">Nome</label><input id="nome-colecao" value={nomeColecao} onChange={(e) => setNomeColecao(e.target.value)} required /><label htmlFor="descricao-colecao">Descrição</label><input id="descricao-colecao" value={descricaoColecao} onChange={(e) => setDescricaoColecao(e.target.value)} /><button type="submit">Criar coleção</button></form></div><section className="collection-list">{colecoes.length === 0 ? <div className="panel-card"><h2>Nenhuma coleção</h2><p>Crie uma lista para organizar jogos por tema, prioridade ou momento.</p></div> : colecoes.map((colecao) => <article className="panel-card" key={colecao.id}><h2>{colecao.nome}</h2><p>{colecao.descricao}</p><label htmlFor={`jogo-${colecao.id}`}>Adicionar jogo</label><select id={`jogo-${colecao.id}`} defaultValue="" onChange={(e) => { adicionarNaColecao(colecao.id, e.target.value); e.target.value = ''; }}><option value="">Selecione da biblioteca</option>{itens.filter((jogo) => !colecao.jogos.some((v) => v.id === jogo.jogoId)).map((jogo) => <option key={jogo.jogoId} value={jogo.jogoId}>{jogo.titulo}</option>)}</select><div className="tag-list">{colecao.jogos.map((jogo) => <span key={jogo.id}>{jogo.titulo}</span>)}</div></article>)}</section></section></main>}

      {tela === 'configuracoes' && <main id="configuracoes"><header className="page-header"><div><span className="eyebrow">PREFERÊNCIAS LOCAIS</span><h1>Configurações</h1><p>Personalize como o GameLife aparece neste computador.</p></div></header><section className="settings-card form-card">{carregandoPerfil ? <p>Carregando perfil...</p> : <form onSubmit={salvarPerfil}><label htmlFor="nome-perfil">Nome de usuário</label><input id="nome-perfil" value={perfil.nome || ''} onChange={(e) => setPerfil((atual) => ({ ...atual, nome: e.target.value }))} required /><label htmlFor="foto-perfil">Foto do perfil</label><input id="foto-perfil" className="file-input" type="file" accept="image/*" onChange={(e) => carregarFoto(e.target.files[0])} /><label className="setting-toggle"><input type="checkbox" checked={perfil.exibirJogosRecentes ?? true} onChange={(e) => setPerfil((atual) => ({ ...atual, exibirJogosRecentes: e.target.checked }))} /> Exibir jogos recentes no início</label>{mensagem && <div className={`feedback ${mensagem.tipo}`} role="alert">{mensagem.texto}</div>}<button type="submit">Salvar configurações</button></form>}</section></main>}
    </div>
  );
}

export default App;
