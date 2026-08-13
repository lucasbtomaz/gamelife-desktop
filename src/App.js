import { useEffect, useState } from 'react';
import './App.css';

const plataformas = ['PlayStation 5', 'PlayStation 4', 'PlayStation 3', 'PlayStation 2', 'Xbox Series X|S', 'Xbox One', 'Nintendo Switch', 'PC', 'Outra'];

const Icon = ({ children, size = 20 }) => (
  <svg aria-hidden="true" width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
    {children}
  </svg>
);

function App() {
  const [itens, setItens] = useState([]);
  const [titulo, setTitulo] = useState('');
  const [plataforma, setPlataforma] = useState('');
  const [midiaFisica, setMidiaFisica] = useState(false);
  const [carregando, setCarregando] = useState(true);
  const [salvando, setSalvando] = useState(false);
  const [mensagem, setMensagem] = useState(null);

  useEffect(() => {
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
  }, []);

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

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <a className="brand" href="#biblioteca" aria-label="GameLife">
          <span className="brand-mark"><Icon size={25}><path d="M7 6h10l3 9a2.3 2.3 0 0 1-4 2l-2-2h-4l-2 2a2.3 2.3 0 0 1-4-2z"/><path d="M8 10v4M6 12h4M16 11h.01M18 13h.01"/></Icon></span>
          <span>Game<span>Life</span></span>
        </a>

        <nav aria-label="Menu principal">
          <a className="nav-item active" href="#biblioteca">
            <Icon><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"/><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"/></Icon>
            Biblioteca
          </a>
        </nav>

        <div className="sidebar-footer">
          <span className="avatar">GL</span>
          <span><strong>Minha coleção</strong><small>{itens.length} {itens.length === 1 ? 'jogo' : 'jogos'}</small></span>
        </div>
      </aside>

      <main id="biblioteca">
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
      </main>
    </div>
  );
}

export default App;
